using System.Linq;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters
{
    public class MessageTypeFilter : DispatcherFilterAttribute
    {
        private readonly MessageType[] _type;

        public MessageTypeFilter(params MessageType[] type) {
            _type = type;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData)
        {
            return update.GetMessage() != null && _type.Contains(update.GetMessage().Type);
        }
    }
}
