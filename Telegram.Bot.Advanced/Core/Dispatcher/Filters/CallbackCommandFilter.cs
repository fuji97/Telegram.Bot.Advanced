using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Core.Tools;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters;

public sealed class CallbackCommandFilter : DispatcherFilterAttribute {
    private readonly string[] _commands;

    public CallbackCommandFilter(params string[] commands) {
        this._commands = commands;
    }

    public override bool IsValid(Update update, TelegramChat? chat, MessageCommand command, ITelegramBotData botData) {
        var data = update.CallbackQuery?.Data;
        if (data is null) {
            return false;
        }

        return _commands.Contains(InlineDataWrapper.ParseInlineData(data).Command);
    }
}
