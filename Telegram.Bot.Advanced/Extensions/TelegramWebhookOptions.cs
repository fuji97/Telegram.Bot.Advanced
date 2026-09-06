namespace Telegram.Bot.Advanced.Extensions;

/// <summary>
/// Options for <see cref="ServiceCollectionExtensions.AddTelegramWebhooks"/>. Both <see cref="BaseUri"/> and
/// <see cref="SecretToken"/> are validated at hosted-service startup, not at configuration time.
/// </summary>
public sealed class TelegramWebhookOptions {
    /// <summary>
    /// Public base URL of this server. Combined with each bot's base path and endpoint to build its webhook URL.
    /// </summary>
    public Uri? BaseUri { get; set; }

    /// <summary>
    /// Secret token Telegram must present in the "X-Telegram-Bot-Api-Secret-Token" header on every webhook
    /// request. Never log this value.
    /// </summary>
    public string? SecretToken { get; set; }

    /// <summary>
    /// If true, every bot's webhook is deleted when the host shuts down. Default is false.
    /// </summary>
    public bool DeleteWebhookOnShutdown { get; set; }
}
