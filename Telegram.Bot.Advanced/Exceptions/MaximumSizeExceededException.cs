using System;

namespace Telegram.Bot.Advanced.Exceptions {
    public class MaximumSizeExceededException : Exception {
        public MaximumSizeExceededException() {
        }

        public MaximumSizeExceededException(string message) : base(message) {
        }

        public MaximumSizeExceededException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}