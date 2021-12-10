using System;
using System.Collections.Generic;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Core.Holder {
    public class TelegramBotData : ITelegramBotData {
        public string Endpoint { get; private set; } = null!;
        public ITelegramBotClient Bot { get; private set; } = null!;
        public IDispatcher Dispatcher { get; private set; } = null!;
        public string BasePath { get; private set; } = null!;
        public string? Username { get; set; }
        public UserUpdate UserUpdate { get; set; }
        public IgnoreBehaviour GroupChatBehaviour { get; set; }
        public IgnoreBehaviour PrivateChatBehaviour { get; set; }
        public IList<UserRole> DefaultUserRole { get; set; } = null!;
        public StartupNewsletter? StartupNewsletter { get; set; }

        [Obsolete]
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

            // Try to get bot informations and set its username
            try {
                var result = Bot.GetMeAsync().Result;
                Username = result.Username;
            }
            catch (Exception e) {
                throw new ArgumentException("Can't obtain the bot information.", nameof(Bot), e);
            }
        }

        [Obsolete]
        public TelegramBotData(ITelegramBotClient bot, IDispatcherBuilder dispatcherBuilder, string endpoint,
            string basePath, UserUpdate userUpdate) {
            SetupParameters(bot, dispatcherBuilder.SetTelegramBotData(this).Build(), endpoint, basePath, userUpdate);
        }

        public TelegramBotData(Action<TelegramBotDataOptions> optionsAction) {
            var options = new TelegramBotDataOptions();
            optionsAction.Invoke(options);
            
            Endpoint = options.Endpoint ?? throw new NullReferenceException("Endpoint option when building TelegramBotData cannot be null");
            Bot = options.Bot ?? throw new NullReferenceException("Bot option when building TelegramBotData cannot be null");
            BasePath = options.BasePath;
            UserUpdate = options.UserUpdate;
            GroupChatBehaviour = options.GroupChatBehaviour;
            PrivateChatBehaviour = options.PrivateChatBehaviour;
            DefaultUserRole = options.DefaultUserRole;
            StartupNewsletter = options.StartupNewsletter;
            
            if (options.DispatcherBuilder != null) {
                Dispatcher = options.DispatcherBuilder.SetTelegramBotData(this).Build();
            }
            else {
                Dispatcher = options.Dispatcher ?? throw new NullReferenceException("Either Dispatcher or DispatcherBuild have to be set when building TelegramBotData");
            }

            // Try to get bot information and set its username
            try {
                var result = Bot.GetMeAsync().Result;
                Username = result.Username;
            }
            catch (Exception e) {
                throw new ArgumentException("Can't obtain the bot information.", nameof(Bot), e);
            }
            
            
        }
    }
}