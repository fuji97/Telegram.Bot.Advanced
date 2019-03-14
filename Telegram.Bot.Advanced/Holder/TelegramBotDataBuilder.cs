using Telegram.Bot.Advanced.Dispatcher;

namespace Telegram.Bot.Advanced.Holder {
    public class TelegramBotDataBuilder {
        private string _endpoint;
        private ITelegramBotClient _bot;
        private IDispatcher _dispatcher;
        private IDispatcherBuilder _builder;
        private string _basePath = "/telegram";

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

        public ITelegramBotData Build() {
            if (_builder != null) {
                return new TelegramBotData(_bot, _builder, _endpoint, _basePath);
            }

            return new TelegramBotData(_bot, _dispatcher, _endpoint, _basePath);
        }
    }
}