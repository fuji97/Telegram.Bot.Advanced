using System.Linq;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters
{
    /// <summary>
    /// The method is eligible if the type of the chat matches with one of the passed types
    /// </summary>
    public class ChatTypeFilter : DispatcherFilterAttribute {
        private readonly ChatType[] _type;

        public ChatTypeFilter(params ChatType[] type) {
            _type = type;
        }

        /// <inheritdoc />
        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData) {
            return update.Message != null && _type.Contains(update.Message.Chat.Type);
        }
    }
}
