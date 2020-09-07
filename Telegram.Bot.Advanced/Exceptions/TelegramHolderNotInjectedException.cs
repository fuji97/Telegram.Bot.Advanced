using System;

namespace Telegram.Bot.Advanced.Exceptions {
    
    /// <summary>
    /// Throws if the TelegramHolder was not injected in the DI Container in ConfigureServices
    /// </summary>
    public class TelegramHolderNotInjectedException : Exception {
        public TelegramHolderNotInjectedException() { }
        public TelegramHolderNotInjectedException(string message) : base(message) { }
        public TelegramHolderNotInjectedException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected TelegramHolderNotInjectedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        { }

        public override string ToString() {
            return
                "ITelegramHolder was not injected in the DI container, yuo have to set it by calling IServiceCollection.AddTelegramHolder()";
        }
    }
}