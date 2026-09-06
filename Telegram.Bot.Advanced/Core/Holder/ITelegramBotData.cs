using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Core.Holder;

/// <summary>
/// Immutable configuration plus the mutable runtime state (<see cref="Username"/>) for a Telegram bot registered
/// in the application.
/// </summary>
public interface ITelegramBotData {
    /// <summary>
    /// Route segment that identifies this bot. Validated to be a single segment (no '/', '\', or "..") and to
    /// never look like a Telegram bot token, so it is safe to expose in a URL.
    /// </summary>
    string Endpoint { get; }
    ITelegramBotClient Bot { get; }
    IDispatcher Dispatcher { get; }
    /// <summary>
    /// Normalized to have exactly one leading and one trailing slash, e.g. "/telegram/".
    /// </summary>
    string BasePath { get; }
    /// <summary>
    /// The only runtime-settable property; set once by the hosted service after calling GetMe.
    /// </summary>
    string? Username { get; set; }
    UserUpdate UserUpdate { get; }
    IgnoreBehaviour GroupChatBehaviour { get; }
    IgnoreBehaviour PrivateChatBehaviour { get; }
    IReadOnlyList<UserRole> DefaultUserRole { get; }
    StartupNewsletter? StartupNewsletter { get; }
}
