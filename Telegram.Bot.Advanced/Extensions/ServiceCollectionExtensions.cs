using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.Extensions {
    public static class ServiceCollectionExtensions {
        public static IServiceCollection AddTelegramHolder(this IServiceCollection services,
            params ITelegramBotData[] bots) {
            services.AddSingleton<ITelegramHolder, TelegramHolder>(
                holder => new TelegramHolder(bots)
            );
            
            // Register controllers to DI
            foreach (var bot in bots) {
                bot.Dispatcher.RegisterController(services);
            }

            return services;
        }
/*        
        public static IServiceCollection AddTelegramPolling<TContext, TController>(this IServiceCollection services, params ITelegramBotData[] bots) 
            where TContext : TelegramContext, new() 
            where TController : class, ITelegramController<TContext>, new() {
            services.TryAddSingleton<ITelegramHolder>();
            
            services.AddScoped<TController>();
            return services;
        }
        */
    }
    
}
