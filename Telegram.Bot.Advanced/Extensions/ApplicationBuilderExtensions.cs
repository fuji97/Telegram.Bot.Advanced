using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Core.Middlewares;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Types.Enums;

namespace Telegram.Bot.Advanced.Extensions {
    public static class ApplicationBuilderExtensions {
        public static IApplicationBuilder UseTelegramRouting(this IApplicationBuilder app, TelegramRoutingOptions options = null) {
            var holder = app.ApplicationServices.GetService<ITelegramHolder>();
            if (holder == null) {
                throw new TelegramHolderNotInjectedException();
            }

            foreach (var bot in holder) {
                app.Map(bot.BasePath + bot.Endpoint, 
                    builder => builder.UseMiddleware<TelegramRouting>(bot.Endpoint));
                if (options?.WebhookBaseUrl != null) {
                    bot.Bot.SetWebhookAsync(options.WebhookBaseUrl + bot.BasePath + bot.Endpoint).Start();
                }
                bot.Dispatcher.SetServices(app.ApplicationServices);
                bot.Username = (bot.Bot.GetMeAsync().Result).Username;
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
                bot.Username = (bot.Bot.GetMeAsync().Result).Username;
                bot.Bot.StartReceiving(Array.Empty<UpdateType>());
            }
            return app;
        }

        public static IApplicationBuilder UseStartupNewsletter(this IApplicationBuilder app) {
            using (var scoped = app.ApplicationServices.CreateScope()) {
                var holder = scoped.ServiceProvider.GetService<ITelegramHolder>();
                if (holder == null) {
                    throw new TelegramHolderNotInjectedException();
                }
            
                var newsletterService = scoped.ServiceProvider.GetService<INewsletterService>();
                if (newsletterService == null) {
                    throw new NewsletterServiceNotInjectedException();
                }

                var logger = app.ApplicationServices.GetService<ILogger<IApplicationBuilder>>();

                foreach (var botData in holder) {
                    if (botData.StartupNewsletter != null) {
                        if (newsletterService.GetNewsletterByKey(botData.StartupNewsletter.NewsletterKey) != null) {
                            logger?.LogInformation("Bot @{Username}: sending startup message to newsletter {NewsletterKey}",
                                botData.Username, botData.StartupNewsletter.NewsletterKey);
                            var result = newsletterService.SendNewsletter(botData.StartupNewsletter.NewsletterKey, chat => {
                                botData.StartupNewsletter.Action(botData, chat, app.ApplicationServices);
                            });
                            logger.LogInformation("Sending completed. Result: Successes: {TotalSuccesses} - " +
                                                  "Failures: {TotalErrors}", 
                                result.TotalSuccesses, result.TotalErrors);
                        }
                        else {
                            logger?.LogWarning("The newsletter {NewsletterKey} doesn't exist", botData.StartupNewsletter.NewsletterKey);
                        }
                    }
                }
            }

            return app;
        }
    }
}