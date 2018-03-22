using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Telegram.Bot.Advanced.Exceptions
{
    class InvalidRouteMethodArguments : TelegramBotAdvancedException
    {
        public ParameterInfo InvalidParamer { get; } = null;

        public InvalidRouteMethodArguments() : base() { }

        public InvalidRouteMethodArguments(ParameterInfo param) : base()
        {
            InvalidParamer = param;
        }

        public InvalidRouteMethodArguments(ParameterInfo param, string message) : base(message)
        {
            InvalidParamer = param;
        }

        public InvalidRouteMethodArguments(ParameterInfo param, string message, System.Exception inner) : base(message,
            inner)
        {
            InvalidParamer = param;
        }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected InvalidRouteMethodArguments(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        { }
    }
}
