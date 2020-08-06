using System;
using Telegram.Bot.Advanced.Dispatcher;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Holder {
    public class TelegramBotData : ITelegramBotData {
        public string Endpoint { get; private set; }
        public ITelegramBotClient Bot { get; private set; }
        public IDispatcher Dispatcher { get; private set; }
        public string BasePath { get; private set; }
        public string Username { get; set; }
        public UserUpdate UserUpdate { get; set; }
        public IgnoreBehaviour GroupChatBehaviour { get; set; }
        public IgnoreBehaviour PrivateChatBehaviour { get; set; }

        public TelegramBotData(ITelegramBotClient bot, IDispatcher dispatcher, string endpoint,
            string basePath, UserUpdate userUpdate) {
            SetupParameters(bot, dispatcher, endpoint, basePath, userUpdate);
        }

        private void SetupParameters(ITelegramBotClient bot, IDispatcher dispatcher, string endpoint, string basePath,
            UserUpdate userUpdate) {
            Endpoint = endpoint;
            Bot = bot;
            Dispatcher = dispatcher;
            BasePath = basePath;
            UserUpdate = userUpdate;
        }

        public TelegramBotData(ITelegramBotClient bot, IDispatcherBuilder dispatcherBuilder, string endpoint,
            string basePath, UserUpdate userUpdate) {
            SetupParameters(bot, dispatcherBuilder.SetTelegramBotData(this).Build(), endpoint, basePath, userUpdate);
        }

        public TelegramBotData(Action<TelegramBotDataOptions> optionsAction) {
            var options = new TelegramBotDataOptions();
            optionsAction.Invoke(options);
            if (options.DispatcherBuilder != null) {
                SetupParameters(options.Bot, options.DispatcherBuilder.SetTelegramBotData(this).Build(), 
                    options.Endpoint, options.BasePath, options.UserUpdate);
            }
            else {
                SetupParameters(options.Bot, options.Dispatcher, options.Endpoint, options.BasePath, options.UserUpdate);
            }
            
            
        }
    }
}