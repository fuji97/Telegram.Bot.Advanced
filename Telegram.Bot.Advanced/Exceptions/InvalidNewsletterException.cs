using System;
using System.Runtime.Serialization;

namespace Telegram.Bot.Advanced.Exceptions {
    public class InvalidNewsletterException : TelegramBotAdvancedException {
        public InvalidNewsletterException() {
        }

        public InvalidNewsletterException(string message) : base(message) {
        }

        public InvalidNewsletterException(string message, Exception inner) : base(message, inner) {
        }

        protected InvalidNewsletterException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}