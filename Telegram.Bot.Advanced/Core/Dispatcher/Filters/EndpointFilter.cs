using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters;

/// <summary>
/// The method is eligible if the endpoint of the bot in use match the passed one
/// </summary>
public sealed class EndpointFilter : DispatcherFilterAttribute {
    private readonly string[] _endpoint;

    public EndpointFilter(params string[] endpoint) {
        _endpoint = endpoint;
    }

    /// <inheritdoc />
    public override bool IsValid(Update update, TelegramChat? user, MessageCommand command, ITelegramBotData botData) => _endpoint.Contains(botData?.Endpoint);
}
