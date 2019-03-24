using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Dispatcher.Filters
{
    public class NoCommandFilter : DispatcherFilterAttribute
    {
        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData) {
            return command == null || !command.IsCommand();
        }
    }
}
