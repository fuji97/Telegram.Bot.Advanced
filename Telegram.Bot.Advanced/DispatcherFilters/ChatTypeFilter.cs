using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.DispatcherFilters
{
    public class ChatTypeFilter : DispatcherFilterAttribute {
        private readonly ChatType _type;

        public ChatTypeFilter(ChatType type) {
            _type = type;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command) {
            return update.Message != null && update.Message.Chat.Type == _type;
        }
    }
}
