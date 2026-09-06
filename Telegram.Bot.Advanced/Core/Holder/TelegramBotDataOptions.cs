using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Core.Holder;

public sealed class TelegramBotDataOptions {
    /// <summary>
    /// Mandatory. Route segment used to identify this bot; validated by <see cref="TelegramBotData"/>.
    /// </summary>
    public string? Endpoint { get; set; }
    public ITelegramBotClient? Bot { get; set; }
    public IDispatcher? Dispatcher { get; set; }
    public IDispatcherBuilder? DispatcherBuilder { get; set; }
    public string BasePath { get; set; } = "/telegram";
    public UserUpdate UserUpdate { get; set; } = UserUpdate.PrivateMessage;
    public IgnoreBehaviour GroupChatBehaviour { get; set; } = IgnoreBehaviour.IgnoreNonCommandMessages;
    public IgnoreBehaviour PrivateChatBehaviour { get; set; } = IgnoreBehaviour.IgnoreNothing;
    public IList<UserRole> DefaultUserRole { get; set; } = [];
    public StartupNewsletter? StartupNewsletter { get; set; }

    /// <summary>
    /// Creates the Telegram bot client from its API token. Sets only <see cref="Bot"/>; <see cref="Endpoint"/>
    /// must be set separately since it ends up in the webhook URL and must never be the (secret) token itself.
    /// </summary>
    public void CreateTelegramBotClient(string token) {
        ArgumentException.ThrowIfNullOrEmpty(token);
        Bot = new TelegramBotClient(token);
    }
}
