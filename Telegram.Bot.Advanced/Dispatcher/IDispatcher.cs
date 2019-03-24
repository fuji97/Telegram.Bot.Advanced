using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Dispatcher {
    public interface IDispatcher {
        Type GetControllerType();
        Type GetContextType();
        Task DispatchUpdateAsync(Update update, IServiceProvider provider = null);
        void RegisterController(IServiceCollection services);
        void SetServices(IServiceProvider provider);
    }
}