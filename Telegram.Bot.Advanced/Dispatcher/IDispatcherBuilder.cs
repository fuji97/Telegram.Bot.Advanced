using System;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.Dispatcher {
    /// <summary>
    /// Interface for the builder of the Dispatcher
    /// </summary>
    public interface IDispatcherBuilder {
        /// <summary>
        /// Set the ITelegramBotData to use.
        /// </summary>
        /// <param name="botData">ITelegramBotData to use</param>
        /// <returns></returns>
        IDispatcherBuilder SetTelegramBotData(ITelegramBotData botData);
        
        /// <summary>
        /// Build the Dispatcher.
        /// </summary>
        /// <returns>The built Dispatcher</returns>
        IDispatcher Build();

        /// <summary>
        /// Adds manual classes as controllers for the Dispatcher, they must derive ITelegramController<T> with T as
        /// the TelegramContext in use.
        /// </summary>
        /// <param name="controllers"></param>
        /// <returns></returns>
        public IDispatcherBuilder AddControllers(params Type[] controllers);
    }
}
