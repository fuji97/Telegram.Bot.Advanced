using System;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.DispatcherFilters
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class DispatcherFilterAttribute : Attribute {
        public abstract bool IsValid(Update update, TelegramChat chat, MessageCommand command, ITelegramBotData botData);
    }
}
