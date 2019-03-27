using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                app.Map(bot.BasePath + bot.Endpoint, 
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
                bot.Dispatcher.SetServices(app.ApplicationServices);
                bot.Bot.OnUpdate += (sender, e) => 
                    bot.Dispatcher.DispatchUpdateAsync(e.Update, app.ApplicationServices);
            }
            foreach (var bot in holder) {
                bot.Bot.DeleteWebhookAsync().Wait();
                app.Map(bot.BasePath + bot.Endpoint, 
                    builder => builder.Run(async context => {
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("Ok");
                    }));
                bot.Bot.StartReceiving(Array.Empty<UpdateType>());
            }
            return app;
        }
    }
}