using System;
using System.Collections.Generic;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Core.Holder {
    public class TelegramBotDataOptions {
        public string? Endpoint { get; set; }
        public ITelegramBotClient? Bot { get; set; }
        public IDispatcher? Dispatcher { get; set; }
        public IDispatcherBuilder? DispatcherBuilder { get; set; }
        public string BasePath { get; set; } = "/telegram";
        public UserUpdate UserUpdate { get; set; } = UserUpdate.PrivateMessage;
        public IgnoreBehaviour GroupChatBehaviour { get; set; } = IgnoreBehaviour.IgnoreNonCommandMessages;
        public IgnoreBehaviour PrivateChatBehaviour { get; set; } = IgnoreBehaviour.IgnoreNothing;
        public IList<UserRole> DefaultUserRole { get; set; } = new List<UserRole>();
        public StartupNewsletter? StartupNewsletter { get; set; }

        public void CreateTelegramBotClient(string key) {
            Bot = new TelegramBotClient(key);
            Endpoint ??= key;
        }
    }
}