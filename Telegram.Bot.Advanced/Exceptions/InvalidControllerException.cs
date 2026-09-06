namespace Telegram.Bot.Advanced.Exceptions;

public sealed class InvalidControllerException : TelegramBotAdvancedException {
    public InvalidControllerException() {
    }

    public InvalidControllerException(string message) : base(message) {
    }

    public InvalidControllerException(string message, Exception inner) : base(message, inner) {
    }
}
