using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.Dispatcher {
    /// <summary>
    /// Interface for the builder of the Dispatcher
    /// </summary>
    public interface IDispatcherBuilder {
        /// <summary>
        /// Set the ITelegramBotData to use
        /// </summary>
        /// <param name="botData">ITelegramBotData to use</param>
        /// <returns></returns>
        IDispatcherBuilder SetTelegramBotData(ITelegramBotData botData);
        
        /// <summary>
        /// Build the Dispatcher
        /// </summary>
        /// <returns>The built Dispatcher</returns>
        IDispatcher Build();
    }
}
