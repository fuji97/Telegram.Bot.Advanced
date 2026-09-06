using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Extensions;

namespace Telegram.Bot.Advanced.Core.Hosting;

/// <summary>
/// Registers the Telegram webhook (with its secret token) for every bot at startup, failing host startup if
/// registration fails. Optionally removes each webhook on shutdown; shutdown failures are logged, never thrown.
/// </summary>
internal sealed class TelegramWebhookHostedService : IHostedService {
    private readonly ITelegramHolder _holder;
    private readonly TelegramWebhookOptions _options;
    private readonly ILogger<TelegramWebhookHostedService> _logger;

    public TelegramWebhookHostedService(
        ITelegramHolder holder,
        IOptions<TelegramWebhookOptions> options,
        ILogger<TelegramWebhookHostedService> logger) {
        _holder = holder;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        if (_options.BaseUri is null) {
            throw new InvalidOperationException("TelegramWebhookOptions.BaseUri must be set when using webhook transport.");
        }

        if (string.IsNullOrEmpty(_options.SecretToken)) {
            throw new InvalidOperationException("TelegramWebhookOptions.SecretToken must be set when using webhook transport.");
        }

        var baseUri = _options.BaseUri.AbsoluteUri.EndsWith('/')
            ? _options.BaseUri
            : new Uri(_options.BaseUri.AbsoluteUri + "/", UriKind.Absolute);

        foreach (var bot in _holder) {
            var me = await bot.Bot.GetMe(cancellationToken);
            bot.Username = me.Username;

            var relativePath = $"{bot.BasePath.Trim('/')}/{Uri.EscapeDataString(bot.Endpoint)}";
            var webhookUri = new Uri(baseUri, relativePath);

            try {
                await bot.Bot.SetWebhook(webhookUri.AbsoluteUri, secretToken: _options.SecretToken, cancellationToken: cancellationToken);
            }
            catch (Exception e) {
                throw new InvalidOperationException($"Failed to register the webhook for bot endpoint '{bot.Endpoint}'.", e);
            }

            _logger.LogInformation("Bot @{Username} webhook registered", bot.Username);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        if (!_options.DeleteWebhookOnShutdown) {
            return;
        }

        foreach (var bot in _holder) {
            try {
                await bot.Bot.DeleteWebhook(cancellationToken: cancellationToken);
            }
            catch (Exception e) {
                _logger.LogWarning(e, "Failed to delete the webhook for bot endpoint '{Endpoint}' during shutdown", bot.Endpoint);
            }
        }
    }
}
