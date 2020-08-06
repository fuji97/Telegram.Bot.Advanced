using System.Linq;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Dispatcher.Filters {
    /// <summary>
    /// The method is eligible if the role of the chat is contained in the parameter roles.
    /// </summary>
    public class ChatRoleFilter : DispatcherFilterAttribute {
        private readonly ChatRole[] _role;

        public ChatRoleFilter(params ChatRole[] role) {
            _role = role;
        }

        /// <inheritdoc />
        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData) {
            return user != null && _role.Contains(user.Role);
        }
    }
}