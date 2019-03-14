using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Middlewares;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Extensions {
    public static class ApplicationBuilderExtensions {
        public static IApplicationBuilder UseTelegramRouting(this IApplicationBuilder app) {
            var holder = app.ApplicationServices.GetService<ITelegramHolder>();
            if (holder == null) {
                throw new TelegramHolderNotInjectedException();
            }

            foreach (var bot in holder) {
                app.Map(Path.Combine(bot.BasePath, bot.Endpoint), 
                    builder => builder.UseMiddleware<TelegramRouting>(bot.Endpoint));
                bot.Dispatcher.SetServices(app.ApplicationServices);
            }
            return app;
        }
        
        public static IApplicationBuilder UseTelegramPolling(this IApplicationBuilder app) {
            var holder = app.ApplicationServices.GetService<ITelegramHolder>();
            if (holder == null) {
                throw new TelegramHolderNotInjectedException();
            }

            foreach (var bot in holder) {
                bot.Bot.OnUpdate += (sender, e) => 
                    bot.Dispatcher.DispatchUpdateAsync(e.Update, app.ApplicationServices);
            }
            foreach (var bot in holder) {
                bot.Bot.StartReceiving(Array.Empty<UpdateType>());
            }
            return app;
        }
    }
}