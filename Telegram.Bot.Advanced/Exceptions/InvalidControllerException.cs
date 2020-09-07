using System;
using System.Runtime.Serialization;

namespace Telegram.Bot.Advanced.Exceptions {
    public class InvalidControllerException : TelegramBotAdvancedException {
        public InvalidControllerException() {
        }

        public InvalidControllerException(string message) : base(message) {
        }

        public InvalidControllerException(string message, Exception inner) : base(message, inner) {
        }

        protected InvalidControllerException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}