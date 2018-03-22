using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Telegram.Bot.Advanced.Exceptions
{
    public class TelegramBotAdvancedException : Exception {
        public TelegramBotAdvancedException() : base() { }
        public TelegramBotAdvancedException(string message) : base(message) { }
        public TelegramBotAdvancedException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected TelegramBotAdvancedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        { }
    }
}
