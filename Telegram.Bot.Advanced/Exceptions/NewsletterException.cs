namespace Telegram.Bot.Advanced.Exceptions;

/// <summary>
/// Thrown when a newsletter operation references a newsletter or chat that doesn't exist
/// </summary>
public sealed class NewsletterException : TelegramBotAdvancedException {
    public NewsletterException() {
    }

    public NewsletterException(string message) : base(message) {
    }

    public NewsletterException(string message, Exception innerException) : base(message, innerException) {
    }
}
