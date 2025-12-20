using Microsoft.Extensions.Caching.Distributed;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using QuantumDice.Api.Services;
using QuantumDice.Infrastructure.Data;
using QuantumDice.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace QuantumDice.Api.Bot;

/// <summary>
/// Bot 消息处理器
/// </summary>
public class BotUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IPlayerService _playerService;
    private readonly IGameService _gameService;
    private readonly IDealerService _dealerService;
    private readonly QuantumDiceDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<BotUpdateHandler> _logger;

    public BotUpdateHandler(
        ITelegramBotClient botClient,
        IPlayerService playerService,
        IGameService gameService,
        IDealerService dealerService,
        QuantumDiceDbContext db,
        IDistributedCache cache,
        ILogger<BotUpdateHandler> logger)
    {
        _botClient = botClient;
        _playerService = playerService;
        _gameService = gameService;
        _dealerService = dealerService;
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => HandleMessageAsync(update.Message!, ct),
            UpdateType.CallbackQuery => HandleCallbackQueryAsync(update.CallbackQuery!, ct),
            _ => Task.CompletedTask
        };

        await handler;
    }

    private async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        if (message.Chat.Type == ChatType.Private)
        {
            await HandlePrivateMessageAsync(message, ct);
        }
        else if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
        {
            await HandleGroupMessageAsync(message, ct);
        }
    }

    private async Task HandlePrivateMessageAsync(Message message, CancellationToken ct)
    {
        var text = message.Text ?? "";
        var userId = message.From!.Id;

        if (text.StartsWith("/start"))
        {
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "🎲 欢迎使用 QuantumDice!\n\n请在群组中使用本机器人进行游戏。",
                cancellationToken: ct
            );
        }
        else if (text.StartsWith("/help"))
        {
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "📖 帮助说明\n\n" +
                      "/start - 开始\n" +
                      "/balance - 查询余额\n" +
                      "/kf - 联系客服\n",
                cancellationToken: ct
            );
        }
    }

    private async Task HandleGroupMessageAsync(Message message, CancellationToken ct)
    {
        var text = message.Text ?? "";
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;
        var username = message.From.Username;
        var firstName = message.From.FirstName;

        // 优先处理绑定命令
        if (text.StartsWith("/bind ") || text.StartsWith("/绑定 "))
        {
            await HandleBindCommandAsync(message, ct);
            return;
        }

        // 检查群组是否已绑定
        var group = await _db.Groups
            .Include(g => g.Dealer)
            .FirstOrDefaultAsync(g => g.TelegramGroupId == chatId && g.IsActive, ct);

        if (group == null)
        {
            // 未绑定的群组，不响应
            return;
        }

        // 检查庄家订阅
        var isValid = await _dealerService.IsSubscriptionValidAsync(group.DealerId);
        if (!isValid)
        {
            // 订阅过期，不响应
            return;
        }

        // 处理命令
        if (text.StartsWith("/"))
        {
            await HandleGroupCommandAsync(message, group.Id, ct);
            return;
        }

        // 处理投注消息
        if (await TryParseBetAsync(text, chatId, userId, username, firstName, group.Id, ct))
        {
            return;
        }
    }

    private async Task HandleBindCommandAsync(Message message, CancellationToken ct)
    {
        var text = message.Text ?? "";
        var parts = text.Split(' ');
        if (parts.Length < 2) return;

        var code = parts[1].Trim();
        var chatId = message.Chat.Id;
        var chatTitle = message.Chat.Title;

        // 验证验证码
        var dealerIdStr = await _cache.GetStringAsync($"bind_code:{code}", ct);
        if (string.IsNullOrEmpty(dealerIdStr))
        {
            await _botClient.SendMessage(
                chatId: chatId,
                text: "❌ 绑定码无效或已过期",
                cancellationToken: ct
            );
            return;
        }

        var dealerId = int.Parse(dealerIdStr);

        // 查找或创建群组
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.TelegramGroupId == chatId, ct);
        if (group == null)
        {
            group = new Core.Entities.Group
            {
                TelegramGroupId = chatId,
                GroupName = chatTitle,
                DealerId = dealerId,
                IsActive = true,
                BoundAt = DateTime.UtcNow
            };
            _db.Groups.Add(group);
        }
        else
        {
            if (group.IsActive && group.DealerId != dealerId)
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: "❌ 该群组已被其他庄家绑定",
                    cancellationToken: ct
                );
                return;
            }

            // 更新绑定
            group.DealerId = dealerId;
            group.GroupName = chatTitle; // 更新群名
            group.IsActive = true;
            group.BoundAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        
        // 清除验证码 (可选，防止重复使用，但5分钟过期也无妨)
        await _cache.RemoveAsync($"bind_code:{code}", ct);

        await _botClient.SendMessage(
            chatId: chatId,
            text: $"✅ 群组绑定成功！\n庄家ID: {dealerId}\n群组: {chatTitle}",
            cancellationToken: ct
        );
    }

    private async Task HandleGroupCommandAsync(Message message, long groupId, CancellationToken ct)
    {
        var text = message.Text ?? "";
        var chatId = message.Chat.Id;
        var userId = message.From!.Id;
        var username = message.From.Username;
        var firstName = message.From.FirstName;

        if (text.StartsWith("/balance") || text.StartsWith("/余额"))
        {
            var player = await _playerService.GetOrCreatePlayerAsync(userId, groupId, username, firstName);
            await _botClient.SendMessage(
                chatId: chatId,
                text: $"💰 @{username ?? firstName} 余额: {player.Balance:F2}",
                cancellationToken: ct
            );
        }
        else if (text.StartsWith("/play") || text.StartsWith("/游戏"))
        {
            await SendGameMenuAsync(chatId, ct);
        }
        else if (text.StartsWith("/kf") || text.StartsWith("/客服"))
        {
            await _botClient.SendMessage(
                chatId: chatId,
                text: "📞 如需帮助，请联系群管理员。",
                cancellationToken: ct
            );
        }
    }

    private async Task SendGameMenuAsync(long chatId, CancellationToken ct)
    {
        var games = await _gameService.GetGameTypesAsync();
        
        var buttons = games.Select(g => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                $"{GetGameEmoji(g.Code)} {g.Name}", 
                $"game:{g.Id}"
            )
        }).ToArray();

        var keyboard = new InlineKeyboardMarkup(buttons);

        await _botClient.SendMessage(
            chatId: chatId,
            text: "🎲 请选择游戏类型:",
            replyMarkup: keyboard,
            cancellationToken: ct
        );
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callback, CancellationToken ct)
    {
        var data = callback.Data ?? "";
        var chatId = callback.Message!.Chat.Id;
        var userId = callback.From.Id;

        if (data.StartsWith("game:"))
        {
            var gameTypeId = int.Parse(data.Split(':')[1]);
            await SendBetOptionsAsync(chatId, gameTypeId, ct);
        }
        else if (data.StartsWith("bet:"))
        {
            // 处理投注选择
            var parts = data.Split(':');
            var betMethodId = int.Parse(parts[1]);
            await _botClient.SendMessage(
                chatId: chatId,
                text: $"请输入投注金额，格式: 大10 或 小10",
                cancellationToken: ct
            );
        }

        await _botClient.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
    }

    private async Task SendBetOptionsAsync(long chatId, int gameTypeId, CancellationToken ct)
    {
        var gameType = await _db.GameTypes.FindAsync(gameTypeId);
        if (gameType == null) return;

        InlineKeyboardButton[][] buttons;

        // 根据游戏类型显示不同选项
        if (gameType.Code == "K3")
        {
            buttons = new[]
            {
                new[] { 
                    InlineKeyboardButton.WithCallbackData("📈 大", "bet:big"),
                    InlineKeyboardButton.WithCallbackData("📉 小", "bet:small")
                },
                new[] {
                    InlineKeyboardButton.WithCallbackData("🔢 单", "bet:odd"),
                    InlineKeyboardButton.WithCallbackData("🔢 双", "bet:even")
                },
                new[] {
                    InlineKeyboardButton.WithCallbackData("🎯 豹子", "bet:leopard"),
                    InlineKeyboardButton.WithCallbackData("🎰 顺子", "bet:straight")
                }
            };
        }
        else if (gameType.Code == "DragonTiger")
        {
            buttons = new[]
            {
                new[] {
                    InlineKeyboardButton.WithCallbackData("🐉 龙", "bet:dragon"),
                    InlineKeyboardButton.WithCallbackData("🐅 虎", "bet:tiger")
                },
                new[] {
                    InlineKeyboardButton.WithCallbackData("🤝 和", "bet:tie")
                }
            };
        }
        else // MineSweeper
        {
            buttons = new[]
            {
                new[] {
                    InlineKeyboardButton.WithCallbackData("📈 大", "bet:big"),
                    InlineKeyboardButton.WithCallbackData("📉 小", "bet:small")
                },
                new[] {
                    InlineKeyboardButton.WithCallbackData("🔢 单", "bet:odd"),
                    InlineKeyboardButton.WithCallbackData("🔢 双", "bet:even")
                }
            };
        }

        await _botClient.SendMessage(
            chatId: chatId,
            text: $"🎲 {gameType.Name} - 请选择玩法:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct
        );
    }

    private async Task<bool> TryParseBetAsync(
        string text, 
        long chatId, 
        long userId, 
        string? username, 
        string? firstName,
        long groupId,
        CancellationToken ct)
    {
        // 简单的投注解析: 大10, 小10, 龙10, 虎10 等
        var betPatterns = new Dictionary<string, string>
        {
            { "大", "Big" }, { "小", "Small" },
            { "单", "Odd" }, { "双", "Even" },
            { "龙", "Dragon" }, { "虎", "Tiger" },
            { "和", "Tie" }, { "豹子", "Leopard" }
        };

        foreach (var pattern in betPatterns)
        {
            if (text.StartsWith(pattern.Key))
            {
                var amountStr = text.Substring(pattern.Key.Length).Trim();
                if (decimal.TryParse(amountStr, out var amount) && amount > 0)
                {
                    // 获取玩家
                    var player = await _playerService.GetOrCreatePlayerAsync(userId, groupId, username, firstName);
                    
                    if (player.Balance < amount)
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: $"❌ @{username ?? firstName} 余额不足 (当前: {player.Balance:F2})",
                            cancellationToken: ct
                        );
                        return true;
                    }

                    // TODO: 检查是否有进行中的轮次，记录投注
                    await _botClient.SendMessage(
                        chatId: chatId,
                        text: $"✅ @{username ?? firstName} 下注成功: {pattern.Key} {amount}",
                        cancellationToken: ct
                    );
                    return true;
                }
            }
        }

        return false;
    }

    private string GetGameEmoji(string code) => code switch
    {
        "MineSweeper" => "💣",
        "DragonTiger" => "🐉",
        "K3" => "🎰",
        _ => "🎲"
    };
}
