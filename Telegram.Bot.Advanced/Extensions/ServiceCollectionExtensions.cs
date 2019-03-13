using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.Extensions {
    public static class ServiceCollectionExtensions {
        public static IServiceCollection AddTelegramHolder(this IServiceCollection services,
            IEnumerable<TelegramBotData> bots) {
            services.AddSingleton<ITelegramHolder, TelegramHolder>(
                holder => new TelegramHolder(bots)
            );
            
            // Register controllers to DI
            foreach (var bot in bots) {
                bot.Dispatcher.RegisterController(services);
            }

            return services;
        }
    }
}
