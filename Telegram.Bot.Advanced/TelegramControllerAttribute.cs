using System;
using System.Collections.Generic;
using System.Text;

namespace Telegram.Bot.Advanced {
    class TelegramControllerAttribute : Attribute {
        public readonly string Name;

        public TelegramControllerAttribute(string name) {
            Name = name;
        }
    }
}
