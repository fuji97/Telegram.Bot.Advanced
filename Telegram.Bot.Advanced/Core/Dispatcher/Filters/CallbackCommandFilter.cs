using System.Linq;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Core.Tools;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters {
    public class CallbackCommandFilter : DispatcherFilterAttribute {
        private readonly string[] _commands;

        public CallbackCommandFilter(params string[] commands) {
            this._commands = commands;
        }

        public override bool IsValid(Update update, TelegramChat? chat, MessageCommand? command, ITelegramBotData botData) {
            if (update.Type != UpdateType.CallbackQuery) {
                return false;
            }

            var data = InlineDataWrapper.ParseInlineData(update.CallbackQuery.Data);
            return _commands.Contains(data.Command);
        }
    }
}