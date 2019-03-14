using System;

namespace Telegram.Bot.Advanced {
    public class TelegramControllerAttribute : Attribute {
        public readonly string Name;

        public TelegramControllerAttribute(string name) {
            Name = name;
        }
    }
}
