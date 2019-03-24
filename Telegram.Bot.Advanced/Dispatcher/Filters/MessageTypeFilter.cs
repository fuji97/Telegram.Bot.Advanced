﻿using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Dispatcher.Filters
{
    public class MessageTypeFilter : DispatcherFilterAttribute
    {
        private readonly MessageType _type;

        public MessageTypeFilter(MessageType type) {
            _type = type;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData)
        {
            return update.Message?.Type == _type;
        }
    }
}