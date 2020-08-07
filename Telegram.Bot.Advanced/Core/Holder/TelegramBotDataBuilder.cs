using System;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Core.Holder {
    [Obsolete("Use the TelegramBotData constructor with options instead")]
    public class TelegramBotDataBuilder {
        private string _endpoint;
        private ITelegramBotClient _bot;
        private IDispatcher _dispatcher;
        private IDispatcherBuilder _builder;
        private string _basePath = "/telegram";
        private UserUpdate _userUpdate = UserUpdate.PrivateMessage;

        public TelegramBotDataBuilder SetTelegramBotClient(ITelegramBotClient bot) {
            _bot = bot;
            return this;
        }

        public TelegramBotDataBuilder CreateTelegramBotClient(string key) {
            _bot = new TelegramBotClient(key);
            if (_endpoint == null)
                _endpoint = key;

            return this;
        }

        public TelegramBotDataBuilder SetEndpoint(string endpoint) {
            _endpoint = endpoint;

            return this;
        }

        public TelegramBotDataBuilder SetDispatcher(IDispatcher dispatcher) {
            _dispatcher = dispatcher;

            return this;
        }

        public TelegramBotDataBuilder UseDispatcherBuilder(IDispatcherBuilder builder) {
            _builder = builder;

            return this;
        }

        public TelegramBotDataBuilder SetBasePath(string path) {
            _basePath = path;

            return this;
        }

        public TelegramBotDataBuilder SetUserUpdate(UserUpdate userUpdate) {
            _userUpdate = userUpdate;

            return this;
        }

        public ITelegramBotData Build() {
            if (_builder != null) {
                return new TelegramBotData(_bot, _builder, _endpoint, _basePath, _userUpdate);
            }

            return new TelegramBotData(_bot, _dispatcher, _endpoint, _basePath, _userUpdate);
        }
    }
}