using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Hosting;

/// <summary>
/// Starts long-polling for every registered bot: deletes any existing webhook, resolves the bot's username via
/// GetMe, then starts a background receive loop that forwards updates to the bot's dispatcher.
/// </summary>
internal sealed class TelegramPollingHostedService : IHostedService {
    private readonly ITelegramHolder _holder;
    private readonly IServiceProvider _provider;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<TelegramPollingHostedService> _logger;
    private CancellationTokenSource? _receiverCts;

    public TelegramPollingHostedService(
        ITelegramHolder holder,
        IServiceProvider provider,
        IHostApplicationLifetime lifetime,
        ILogger<TelegramPollingHostedService> logger) {
        _holder = holder;
        _provider = provider;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        _receiverCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);
        var receiverToken = _receiverCts.Token;

        foreach (var bot in _holder) {
            await bot.Bot.DeleteWebhook(cancellationToken: cancellationToken);

            var me = await bot.Bot.GetMe(cancellationToken);
            bot.Username = me.Username;

            ReceiverOptions receiverOptions = new() { AllowedUpdates = Update.AllTypes };

            bot.Bot.StartReceiving(
                (client, update, token) => bot.Dispatcher.DispatchUpdateAsync(update, _provider, token),
                (client, exception, token) => bot.Dispatcher.HandleErrorAsync(exception, _provider, token),
                receiverOptions,
                receiverToken);

            _logger.LogInformation("Bot @{Username} started receiving updates via polling", bot.Username);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        _receiverCts?.Cancel();
        _receiverCts?.Dispose();
        return Task.CompletedTask;
    }
}
