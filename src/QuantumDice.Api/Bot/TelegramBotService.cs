using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace QuantumDice.Api.Bot;

/// <summary>
/// Telegram Bot 后台服务
/// </summary>
public class TelegramBotService : BackgroundService
{
    private readonly ILogger<TelegramBotService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _botClient;

    public TelegramBotService(
        ILogger<TelegramBotService> logger,
        IServiceProvider serviceProvider,
        ITelegramBotClient botClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _botClient = botClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[]
            {
                UpdateType.Message,
                UpdateType.CallbackQuery
            },
            DropPendingUpdates = true
        };

        _logger.LogInformation("Telegram Bot 开始运行...");

        await _botClient.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<BotUpdateHandler>();

        try
        {
            await handler.HandleAsync(update, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理更新时发生错误");
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Telegram Bot 发生错误");
        return Task.CompletedTask;
    }
}
