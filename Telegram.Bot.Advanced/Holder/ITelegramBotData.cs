using Telegram.Bot.Advanced.Dispatcher;
using Telegram.Bot.Advanced.Models;

namespace Telegram.Bot.Advanced.Holder {
    public interface ITelegramBotData {
        string Endpoint { get; }
        ITelegramBotClient Bot { get; }
        IDispatcher Dispatcher { get; }
        string BasePath { get; }
        string Username { get; set; }
        UserUpdate UserUpdate { get; set; }
        IgnoreBehaviour GroupChatBehaviour { get; set; }
        IgnoreBehaviour PrivateChatBehaviour { get; set; }
        
    }
}