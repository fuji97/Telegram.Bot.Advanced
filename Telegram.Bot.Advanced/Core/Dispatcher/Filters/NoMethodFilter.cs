using System;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters {
    [AttributeUsage(AttributeTargets.Method)]
    public class NoMethodFilter : Attribute {
    }
}