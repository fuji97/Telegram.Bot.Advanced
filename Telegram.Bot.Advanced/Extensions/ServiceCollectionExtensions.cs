using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.Extensions {
    public static class ServiceCollectionExtensions {
        public static IServiceCollection AddTelegramHolder<TContext, TController>(this IServiceCollection services,
            IEnumerable<TelegramBotData<TContext, TController>> bots)
            where TContext : TelegramContext, new()
            where TController : class, ITelegramController<TContext>, new() {
            services.AddSingleton<ITelegramHolder<TContext, TController>, TelegramHolder<TContext, TController>>(
                holder => new TelegramHolder<TContext, TController>(bots)
            );

            return services;
        }
    }
}
