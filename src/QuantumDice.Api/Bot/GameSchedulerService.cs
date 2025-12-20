using Telegram.Bot;
using Telegram.Bot.Types;
using QuantumDice.Infrastructure.Data;
using QuantumDice.Core.Entities;
using QuantumDice.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace QuantumDice.Api.Bot;

/// <summary>
/// 游戏循环调度器 - 自动开盘/封盘/开奖
/// </summary>
public class GameSchedulerService : BackgroundService
{
    private readonly ILogger<GameSchedulerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _botClient;

    public GameSchedulerService(
        ILogger<GameSchedulerService> logger,
        IServiceProvider serviceProvider,
        ITelegramBotClient botClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _botClient = botClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("游戏调度器开始运行...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessGameRoundsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理游戏轮次时发生错误");
            }

            // 每秒检查一次
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessGameRoundsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuantumDiceDbContext>();
        var now = DateTime.UtcNow;

        // 1. 检查需要封盘的轮次
        var roundsToClose = await db.GameRounds
            .Where(r => r.Status == RoundStatus.Betting && r.CloseTime <= now)
            .ToListAsync(ct);

        foreach (var round in roundsToClose)
        {
            round.Status = RoundStatus.Closed;
            _logger.LogInformation("轮次 {RoundNumber} 已封盘", round.RoundNumber);

            // 发送封盘通知
            var group = await db.Groups.FindAsync(round.GroupId);
            if (group != null)
            {
                try
                {
                    await _botClient.SendMessage(
                        chatId: group.TelegramGroupId,
                        text: $"🚫 第 {round.RoundNumber} 期 停止下注!",
                        cancellationToken: ct
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "发送封盘通知失败");
                }
            }
        }

        await db.SaveChangesAsync(ct);

        // 2. 检查需要开奖的轮次 (封盘后10秒)
        var roundsToDraw = await db.GameRounds
            .Include(r => r.GameType)
            .Include(r => r.Group)
            .Where(r => r.Status == RoundStatus.Closed && r.CloseTime.AddSeconds(10) <= now)
            .ToListAsync(ct);

        foreach (var round in roundsToDraw)
        {
            await DrawRoundAsync(db, round, ct);
        }

        // 3. 检查需要开始新轮次的群组
        await StartNewRoundsIfNeededAsync(db, now, ct);
    }

    private async Task DrawRoundAsync(QuantumDiceDbContext db, GameRound round, CancellationToken ct)
    {
        var diceCount = round.GameType.DiceCount;
        var random = new Random();
        var results = new List<int>();

        // 生成骰子结果
        for (int i = 1; i <= diceCount; i++)
        {
            var value = random.Next(1, 7);
            results.Add(value);

            db.DiceResults.Add(new DiceResult
            {
                RoundId = round.Id,
                DiceIndex = i,
                Value = value
            });
        }

        round.Status = RoundStatus.Drawing;
        round.DrawTime = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        // 发送骰子动画
        try
        {
            foreach (var _ in Enumerable.Range(0, diceCount))
            {
                await _botClient.SendDice(
                    chatId: round.Group.TelegramGroupId,
                    cancellationToken: ct
                );
                await Task.Delay(500, ct); // 间隔发送
            }

            // 发送开奖结果
            var resultText = string.Join(" ", results.Select(r => $"🎲{r}"));
            var total = results.Sum();
            var resultInfo = diceCount == 3 
                ? $"总和: {total} ({(total >= 11 ? "大" : "小")}/{(total % 2 == 1 ? "单" : "双")})"
                : diceCount == 2 
                    ? $"{(results[0] > results[1] ? "龙" : results[0] < results[1] ? "虎" : "和")}"
                    : $"点数: {results[0]}";

            await _botClient.SendMessage(
                chatId: round.Group.TelegramGroupId,
                text: $"🎉 第 {round.RoundNumber} 期 开奖\n\n{resultText}\n{resultInfo}",
                cancellationToken: ct
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "发送开奖消息失败");
        }

        // 结算
        await SettleRoundAsync(db, round, results.ToArray(), ct);
    }

    private async Task SettleRoundAsync(QuantumDiceDbContext db, GameRound round, int[] diceValues, CancellationToken ct)
    {
        var bets = await db.Bets
            .Include(b => b.Player)
            .Include(b => b.BetMethod)
            .Where(b => b.RoundId == round.Id && b.Status == BetStatus.Pending)
            .ToListAsync(ct);

        var winners = new List<(string Name, decimal Amount)>();

        foreach (var bet in bets)
        {
            var isWin = CheckWin(bet.BetMethod.Code, diceValues);

            if (isWin)
            {
                bet.Status = BetStatus.Won;
                bet.WinAmount = bet.Amount * bet.Odds;
                bet.Player.Balance += bet.WinAmount;
                bet.Player.TotalWin += bet.WinAmount;
                winners.Add((bet.Player.Username ?? bet.Player.FirstName ?? "玩家", bet.WinAmount));

                db.Transactions.Add(new Transaction
                {
                    PlayerId = bet.PlayerId,
                    Type = TransactionType.Win,
                    Amount = bet.WinAmount,
                    BalanceBefore = bet.Player.Balance - bet.WinAmount,
                    BalanceAfter = bet.Player.Balance,
                    RefType = "Bets",
                    RefId = bet.Id
                });
            }
            else
            {
                bet.Status = BetStatus.Lost;
            }
        }

        round.Status = RoundStatus.Settled;
        await db.SaveChangesAsync(ct);

        // 发送中奖榜单
        if (winners.Any())
        {
            var winnerList = string.Join("\n", winners.Select(w => $"🏆 {w.Name}: +{w.Amount:F2}"));
            try
            {
                await _botClient.SendMessage(
                    chatId: round.Group.TelegramGroupId,
                    text: $"💰 中奖榜单:\n\n{winnerList}",
                    cancellationToken: ct
                );
            }
            catch { }
        }

        _logger.LogInformation("轮次 {RoundNumber} 已结算, 中奖人数: {Count}", round.RoundNumber, winners.Count);
    }

    private async Task StartNewRoundsIfNeededAsync(QuantumDiceDbContext db, DateTime now, CancellationToken ct)
    {
        // 获取所有活跃的群组配置
        var scheduleConfigs = await db.GroupScheduleConfigs
            .Include(c => c.Group)
                .ThenInclude(g => g.Dealer)
            .Include(c => c.GameType)
            .Where(c => c.IsEnabled && c.Group.IsActive)
            .ToListAsync(ct);

        foreach (var config in scheduleConfigs)
        {
            // 检查庄家订阅
            var isValid = await db.Subscriptions
                .AnyAsync(s => s.DealerId == config.Group.DealerId 
                    && s.Status == SubscriptionStatus.Active 
                    && s.EndTime > now, ct);

            if (!isValid) continue;

            // 检查是否有进行中的轮次
            var hasActiveRound = await db.GameRounds
                .AnyAsync(r => r.GroupId == config.GroupId 
                    && r.GameTypeId == config.GameTypeId 
                    && (r.Status == RoundStatus.Betting || r.Status == RoundStatus.Closed || r.Status == RoundStatus.Drawing), ct);

            if (hasActiveRound) continue;

            // 检查上一轮结束时间
            var lastRound = await db.GameRounds
                .Where(r => r.GroupId == config.GroupId && r.GameTypeId == config.GameTypeId)
                .OrderByDescending(r => r.DrawTime)
                .FirstOrDefaultAsync(ct);

            var shouldStart = lastRound == null || 
                (lastRound.DrawTime.HasValue && lastRound.DrawTime.Value.AddMinutes(config.IntervalMinutes) <= now);

            if (shouldStart)
            {
                // 开始新轮次
                var roundNumber = now.ToString("yyyyMMddHHmmss");
                var newRound = new GameRound
                {
                    GroupId = config.GroupId,
                    GameTypeId = config.GameTypeId,
                    RoundNumber = roundNumber,
                    Status = RoundStatus.Betting,
                    OpenTime = now,
                    CloseTime = now.AddMinutes(config.IntervalMinutes).AddSeconds(-30)
                };

                db.GameRounds.Add(newRound);
                await db.SaveChangesAsync(ct);

                // 发送开盘通知
                try
                {
                    var timeLeft = (newRound.CloseTime - now).TotalSeconds;
                    await _botClient.SendMessage(
                        chatId: config.Group.TelegramGroupId,
                        text: $"🎲 {config.GameType.Name} 第 {roundNumber} 期 开始!\n\n⏰ 距封盘: {timeLeft:F0} 秒\n📝 请下注...",
                        cancellationToken: ct
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "发送开盘通知失败");
                }

                _logger.LogInformation("群组 {GroupId} 开始新轮次: {RoundNumber}", config.GroupId, roundNumber);
            }
        }
    }

    private bool CheckWin(string betMethodCode, int[] diceValues)
    {
        var total = diceValues.Sum();
        var isLeopard = diceValues.Length == 3 && diceValues.Distinct().Count() == 1;

        return betMethodCode switch
        {
            "Big" => total >= 11 && total <= 17 && !isLeopard,
            "Small" => total >= 4 && total <= 10 && !isLeopard,
            "Odd" => total % 2 == 1 && !isLeopard,
            "Even" => total % 2 == 0 && !isLeopard,
            "Leopard" => isLeopard,
            "Dragon" => diceValues.Length >= 2 && diceValues[0] > diceValues[^1],
            "Tiger" => diceValues.Length >= 2 && diceValues[0] < diceValues[^1],
            "Tie" => diceValues.Length >= 2 && diceValues[0] == diceValues[^1],
            _ => false
        };
    }
}
