using System;

namespace Telegram.Bot.Advanced.Exceptions {
    public class TelegramHolderNotInjectedException : Exception {
        public TelegramHolderNotInjectedException() : base() { }
        public TelegramHolderNotInjectedException(string message) : base(message) { }
        public TelegramHolderNotInjectedException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected TelegramHolderNotInjectedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        { }

        public override string ToString() {
            return
                "ITelegramHolder was not injected in the Depdendency Injection, yuo have to set it by calling IServiceProvider.AddTelegramHolder()";
        }
    }
}