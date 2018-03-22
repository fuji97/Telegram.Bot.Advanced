using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.DispatcherFilters
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class DispatcherFilterAttribute : Attribute {
        public abstract bool IsValid(Update update);
    }
}
