using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Dispatcher.Filters {
    public class ParametersFilter : DispatcherFilterAttribute {
        private readonly int _qnt;

        public ParametersFilter(int qnt) {
            _qnt = qnt;
        }
        
        public override bool IsValid(Update update, TelegramChat chat, MessageCommand command, ITelegramBotData botData) {
            return command.Parameters.Count == _qnt;
        }
    }
}