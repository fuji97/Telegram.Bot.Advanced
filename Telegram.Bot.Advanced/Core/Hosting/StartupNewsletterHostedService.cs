using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Services;

namespace Telegram.Bot.Advanced.Core.Hosting;

/// <summary>
/// Sends each bot's configured startup newsletter once the transport hosted service has started. Registered after
/// the selected transport so it runs once the bot is already reachable.
/// </summary>
internal sealed class StartupNewsletterHostedService : IHostedService {
    private readonly ITelegramHolder _holder;
    private readonly IServiceProvider _provider;
    private readonly ILogger<StartupNewsletterHostedService> _logger;

    public StartupNewsletterHostedService(
        ITelegramHolder holder,
        IServiceProvider provider,
        ILogger<StartupNewsletterHostedService> logger) {
        _holder = holder;
        _provider = provider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        foreach (var bot in _holder) {
            var startupNewsletter = bot.StartupNewsletter;
            if (startupNewsletter is null) {
                continue;
            }

            await using var scope = _provider.CreateAsyncScope();
            var newsletterService = scope.ServiceProvider.GetRequiredService<INewsletterService>();

            var newsletter = await newsletterService.GetNewsletterByKeyAsync(startupNewsletter.NewsletterKey, cancellationToken);
            if (newsletter is null) {
                _logger.LogWarning(
                    "Bot @{Username}: startup newsletter '{NewsletterKey}' does not exist",
                    bot.Username, startupNewsletter.NewsletterKey);
                continue;
            }

            _logger.LogInformation(
                "Bot @{Username}: sending startup message to newsletter '{NewsletterKey}'",
                bot.Username, startupNewsletter.NewsletterKey);

            var result = await newsletterService.SendNewsletterAsync(
                startupNewsletter.NewsletterKey,
                (chat, token) => startupNewsletter.Action(bot, chat, scope.ServiceProvider, token),
                cancellationToken);

            _logger.LogInformation(
                "Startup newsletter '{NewsletterKey}' sent. Successes: {TotalSuccesses} - Errors: {TotalErrors}",
                startupNewsletter.NewsletterKey, result.TotalSuccesses, result.TotalErrors);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
