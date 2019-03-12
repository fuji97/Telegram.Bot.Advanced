using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.DispatcherFilters
{
    public class CommandFilter : DispatcherFilterAttribute {
        private readonly string _command;

        public CommandFilter(string command) {
            _command = command;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command) {
            return command?.Command == _command;
        }
    }
}
