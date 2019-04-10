using System;

namespace Telegram.Bot.Advanced.Exceptions
{
    /// <summary>
    /// Base exception class, all framework exceptions extends this class 
    /// </summary>
    public class TelegramBotAdvancedException : Exception {
        public TelegramBotAdvancedException() { }
        public TelegramBotAdvancedException(string message) : base(message) { }
        public TelegramBotAdvancedException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected TelegramBotAdvancedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        { }
    }
}
