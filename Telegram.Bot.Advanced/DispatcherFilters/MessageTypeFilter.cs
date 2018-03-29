using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.DispatcherFilters
{
    public class MessageTypeFilter : DispatcherFilterAttribute
    {
        private readonly MessageType _type;

        public MessageTypeFilter(MessageType type) {
            _type = type;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command)
        {
            return update.Message?.Type == _type;
        }
    }
}
