namespace Telegram.Bot.Advanced.Exceptions;

/// <summary>
/// Base exception class, all framework exceptions extends this class
/// </summary>
public class TelegramBotAdvancedException : Exception {
    public TelegramBotAdvancedException() { }
    public TelegramBotAdvancedException(string message) : base(message) { }
    public TelegramBotAdvancedException(string message, Exception inner) : base(message, inner) { }
}
