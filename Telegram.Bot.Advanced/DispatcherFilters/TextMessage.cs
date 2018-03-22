using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.DispatcherFilters
{
    public class TextMessage : DispatcherFilterAttribute
    {
        private readonly string _text = null;

        public TextMessage(string text) {
            _text = text;
        }

        public override bool IsValid(Update update) {
            if (update.Message == null)
                return false;
            return update.Message.Text == _text;
        }
    }
}
