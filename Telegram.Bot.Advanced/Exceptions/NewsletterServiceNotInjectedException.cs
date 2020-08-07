using System;

namespace Telegram.Bot.Advanced.Exceptions {
    public class NewsletterServiceNotInjectedException : TelegramBotAdvancedException {
        public NewsletterServiceNotInjectedException() { }
        public NewsletterServiceNotInjectedException(string message) : base(message) { }
        public NewsletterServiceNotInjectedException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected NewsletterServiceNotInjectedException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        { }

        public override string ToString() {
            return
                "INewsletterService was not injected in the DI Container, yuo have to set it by calling IServiceCollection.AddNewsletter()";
        }
    }
}