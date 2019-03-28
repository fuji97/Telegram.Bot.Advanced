using System;

namespace Telegram.Bot.Advanced.Dispatcher.Filters {
    [AttributeUsage(AttributeTargets.Method)]
    public class NoMethodFilter : Attribute {
    }
}