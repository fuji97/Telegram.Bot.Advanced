﻿using System.Linq;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Dispatcher.Filters
{
    public class ChatStateFilter : DispatcherFilterAttribute {
        private readonly int[] _state;

        public ChatStateFilter(params int[] state) {
            _state = state;
        }

        public override bool IsValid(Update update, TelegramChat user, MessageCommand command, ITelegramBotData botData) {
            return user != null && _state.Contains(user.State);
        }
    }
}
