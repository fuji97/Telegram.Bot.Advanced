using System;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Dispatcher.Filters
{
    /// <summary>
    /// Basic class to extends to create a new filter
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class DispatcherFilterAttribute : Attribute {
        /// <summary>
        /// Called when searching for eligible handler controller and method, return true if the method is eligible
        /// </summary>
        /// <param name="update">Update of the current request</param>
        /// <param name="chat">Current chat</param>
        /// <param name="command">MessageCommand generated from current Update</param>
        /// <param name="botData">Data of the bot in use</param>
        /// <returns>If true, the mehod is eligible for the currrent update. If false, the method is discarded</returns>
        public abstract bool IsValid(Update update, TelegramChat chat, MessageCommand command, ITelegramBotData botData);

        /// <summary>
        /// Called when searching for eligible handler controller, return true if the controller is eligible.
        /// This method calls the generic IsValid(), override it to implements custom behaviour for controllers only.
        /// </summary>
        /// <param name="update">Update of the current request</param>
        /// <param name="chat">Current chat</param>
        /// <param name="command">MessageCommand generated from current Update</param>
        /// <param name="botData">Data of the bot in use</param>
        /// <returns>If true, the controller is eligible for the current update. If false, the method is discarded</returns>
        public virtual bool IsValidController(Update update, TelegramChat chat, MessageCommand command,
            ITelegramBotData botData) {
            return IsValid(update, chat, command, botData);
        }
        
        /// <summary>
        /// Called when searching for eligible handler method, return true if the method is eligible.
        /// This method calls the generic IsValid(), override it to implements custom behaviour for methods only.
        /// </summary>
        /// <param name="update">Update of the current request</param>
        /// <param name="chat">Current chat</param>
        /// <param name="command">MessageCommand generated from current Update</param>
        /// <param name="botData">Data of the bot in use</param>
        /// <returns>If true, the method is eligible for the current update. If false, the method is discarded</returns>
        public virtual bool IsValidMethod(Update update, TelegramChat chat, MessageCommand command,
            ITelegramBotData botData) {
            return IsValid(update, chat, command, botData);
        }
    }
}
