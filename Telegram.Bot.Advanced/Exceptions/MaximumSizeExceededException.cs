using System;
using System.Runtime.Serialization;

namespace Telegram.Bot.Advanced.Exceptions {
    public class MaximumSizeExceededException : Exception {
        public MaximumSizeExceededException() {
        }

        protected MaximumSizeExceededException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }

        public MaximumSizeExceededException(string message) : base(message) {
        }

        public MaximumSizeExceededException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}