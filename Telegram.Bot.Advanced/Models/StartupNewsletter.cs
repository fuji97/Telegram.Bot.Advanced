using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Models;

public sealed class StartupNewsletter {
    public string NewsletterKey { get; }

    /// <summary>
    /// Invoked once per subscribed chat by the startup-newsletter hosted service, inside an async scope, with the
    /// host's cancellation token.
    /// </summary>
    public Func<ITelegramBotData, TelegramChat, IServiceProvider, CancellationToken, Task> Action { get; }

    public StartupNewsletter(string newsletterKey, Func<ITelegramBotData, TelegramChat, IServiceProvider, CancellationToken, Task> action) {
        NewsletterKey = newsletterKey;
        Action = action;
    }
}
