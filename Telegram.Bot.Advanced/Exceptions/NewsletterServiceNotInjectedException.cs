namespace Telegram.Bot.Advanced.Exceptions;

/// <summary>
/// Throws if the INewsletterService was not injected in the DI Container in ConfigureServices
/// </summary>
public sealed class NewsletterServiceNotInjectedException : TelegramBotAdvancedException {
    private const string DefaultMessage =
        "INewsletterService was not injected in the DI Container, you have to set it by calling IServiceCollection.AddNewsletter()";

    public NewsletterServiceNotInjectedException() : base(DefaultMessage) { }
    public NewsletterServiceNotInjectedException(string message) : base(message) { }
    public NewsletterServiceNotInjectedException(string message, Exception inner) : base(message, inner) { }
}
