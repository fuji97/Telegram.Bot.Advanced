using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters;

public sealed class UpdateTypeFilter : DispatcherFilterAttribute {
    private readonly UpdateType[] _type;

    public UpdateTypeFilter(params UpdateType[] type) {
        _type = type;
    }

    public override bool IsValid(Update update, TelegramChat? chat, MessageCommand command, ITelegramBotData botData) => _type.Contains(update.Type);
}
