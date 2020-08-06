using System;
using System.Runtime.Serialization;

namespace Telegram.Bot.Advanced.Exceptions
{
    /// <summary>
    /// Throws when trying to add a data in TelegramChat that have the same key of an existing one
    /// </summary>
    public class DuplicateDataKeyException : TelegramBotAdvancedException {
        public DuplicateDataKeyException() {
        }

        public DuplicateDataKeyException(string message) : base(message) {
        }

        public DuplicateDataKeyException(string message, Exception inner) : base(message, inner) {
        }

        protected DuplicateDataKeyException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}
