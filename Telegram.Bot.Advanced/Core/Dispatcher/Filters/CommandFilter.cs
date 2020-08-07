using System.Linq;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters
{
    /// <summary>
    /// The method is eligible if the command of the of the message matches with one of the passed commands
    /// </summary>
    public class CommandFilter : DispatcherFilterAttribute {
        private readonly string[] _command;

        public CommandFilter(params string[] command) {
            _command = command;
        }

        /// <inheritdoc />
        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData) {
            return _command.Contains(command?.Command);
        }
    }
}
