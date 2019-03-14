using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Dispatcher {
    public interface IDispatcher {
        void DispatchUpdate(Update update, IServiceProvider provider);
        Task DispatchUpdateAsync(Update update, IServiceProvider provider);
        void RegisterController(IServiceCollection services);
        void SetServices(IServiceProvider provider);
    }
}