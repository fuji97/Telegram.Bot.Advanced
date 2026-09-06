namespace Telegram.Bot.Advanced.Exceptions;

/// <summary>
/// Throws if the TelegramHolder was not injected in the DI Container in ConfigureServices
/// </summary>
public sealed class TelegramHolderNotInjectedException : TelegramBotAdvancedException {
    private const string DefaultMessage =
        "ITelegramHolder was not injected in the DI container, you have to set it by calling IServiceCollection.AddTelegramHolder()";

    public TelegramHolderNotInjectedException() : base(DefaultMessage) { }
    public TelegramHolderNotInjectedException(string message) : base(message) { }
    public TelegramHolderNotInjectedException(string message, Exception inner) : base(message, inner) { }
}
