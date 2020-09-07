using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Controller {
    /// <summary>
    /// Interface that a Telegram controller have to implements.
    /// This interdace define parameters that are automatically assigned by the Dispatcher during it's creation.
    /// </summary>
    /// <typeparam name="TContext">Telegram context used by the application</typeparam>
    public interface ITelegramController<TContext> where TContext : TelegramContext {
        /// <summary>
        /// If the update is a command, this field contains it's command and parameters (words splitted by spaces)
        /// </summary>
        MessageCommand MessageCommand { set; }
        /// <summary>
        /// Context used by the application
        /// </summary>
        TContext TelegramContext { set; }
        /// <summary>
        /// Instance of TelegramChat of the current chat, connected to TelegramContext
        /// </summary>
        TelegramChat TelegramChat { set; }
        /// <summary>
        /// Data of the bot in use
        /// </summary>
        ITelegramBotData BotData { set; }
        /// <summary>
        /// Raw Update
        /// </summary>
        /// <see cref="Types.Update"/>
        Update Update { set; }
    }
}