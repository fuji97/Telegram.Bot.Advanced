using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters
{
    public class NoCommandFilter : DispatcherFilterAttribute
    {
        public override bool IsValid(Update update, TelegramChat? user, MessageCommand command, ITelegramBotData botData) {
            return command == null || !command.IsCommand();
        }
    }
}
