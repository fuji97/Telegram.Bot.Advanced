using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced {
    public interface IDispatcher {
        void DispatchUpdate(Update update);
        Task DispatchUpdateAsync(Update update);
    }
}