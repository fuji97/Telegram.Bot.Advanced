using System.Linq;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters {
    /// <summary>
    /// The method is eligible if the role of the chat is contained in the parameter roles.
    /// </summary>
    public class ChatRoleFilter : DispatcherFilterAttribute {
        private readonly ChatRole[] _role;

        public ChatRoleFilter(params ChatRole[] role) {
            _role = role;
        }

        /// <inheritdoc />
        public override bool IsValid(Update update, TelegramChat? user, MessageCommand command, ITelegramBotData botData) {
            return user != null && _role.Contains(user.Role);
        }
    }
}