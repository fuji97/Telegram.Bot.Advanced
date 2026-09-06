using System.Text.RegularExpressions;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Core.Holder;

/// <summary>
/// Immutable configuration and runtime state for a single Telegram bot registered in the application.
/// Construction performs no network I/O; the bot's <see cref="Username"/> is resolved and assigned later by the
/// transport hosted service after a successful GetMe call.
/// </summary>
public sealed partial class TelegramBotData : ITelegramBotData {
    public string Endpoint { get; }
    public ITelegramBotClient Bot { get; }
    public IDispatcher Dispatcher { get; }
    public string BasePath { get; }
    public string? Username { get; set; }
    public UserUpdate UserUpdate { get; }
    public IgnoreBehaviour GroupChatBehaviour { get; }
    public IgnoreBehaviour PrivateChatBehaviour { get; }
    public IReadOnlyList<UserRole> DefaultUserRole { get; }
    public StartupNewsletter? StartupNewsletter { get; }

    public TelegramBotData(Action<TelegramBotDataOptions> optionsAction) {
        ArgumentNullException.ThrowIfNull(optionsAction);

        var options = new TelegramBotDataOptions();
        optionsAction(options);

        ValidateEndpoint(options.Endpoint);
        Endpoint = options.Endpoint!;

        Bot = options.Bot ?? throw new ArgumentException(
            "Bot is required; call TelegramBotDataOptions.CreateTelegramBotClient(token) or set Bot directly.",
            nameof(optionsAction));

        BasePath = NormalizeBasePath(options.BasePath);
        UserUpdate = options.UserUpdate;
        GroupChatBehaviour = options.GroupChatBehaviour;
        PrivateChatBehaviour = options.PrivateChatBehaviour;
        DefaultUserRole = [.. options.DefaultUserRole];
        StartupNewsletter = options.StartupNewsletter;

        if (options.DispatcherBuilder is not null) {
            Dispatcher = options.DispatcherBuilder.SetTelegramBotData(this).Build();
        }
        else if (options.Dispatcher is not null) {
            Dispatcher = options.Dispatcher;
        }
        else {
            throw new InvalidOperationException(
                "Either TelegramBotDataOptions.Dispatcher or TelegramBotDataOptions.DispatcherBuilder must be set.");
        }
    }

    private static void ValidateEndpoint(string? endpoint) {
        if (string.IsNullOrWhiteSpace(endpoint)) {
            throw new ArgumentException("Endpoint is required and cannot be null, empty, or whitespace.", nameof(endpoint));
        }

        if (endpoint.Contains('/') || endpoint.Contains('\\') || endpoint.Contains('{') || endpoint.Contains('}')) {
            throw new ArgumentException(
                "Endpoint must be a single literal route segment and cannot contain '/', '\\', '{', or '}'.", nameof(endpoint));
        }

        if (endpoint.Contains("..", StringComparison.Ordinal)) {
            throw new ArgumentException("Endpoint cannot contain '..'.", nameof(endpoint));
        }

        if (TokenShapedEndpointRegex().IsMatch(endpoint)) {
            throw new ArgumentException(
                "Endpoint looks like a Telegram bot token; using a token as the endpoint would leak it in the URL.",
                nameof(endpoint));
        }
    }

    private static string NormalizeBasePath(string basePath) {
        var trimmed = basePath.Trim('/');
        return trimmed.Length == 0 ? "/" : $"/{trimmed}/";
    }

    // Matches the "digits:secret" shape of a Telegram bot API token, e.g. "123456:ABC-DEF...".
    [GeneratedRegex(@"^\d+:.+$")]
    private static partial Regex TokenShapedEndpointRegex();
}
