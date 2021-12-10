using System.Threading;
using Flurl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Core.Middlewares;
using Telegram.Bot.Advanced.Core.Tools;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Extensions {
    public static class ApplicationBuilderExtensions {
        public static IApplicationBuilder UseTelegramRouting(this IApplicationBuilder app, TelegramRoutingOptions? options = null) {
            var holder = app.ApplicationServices.GetService<ITelegramHolder>();
            var logger = app.ApplicationServices.GetService<ILogger<IApplicationBuilder>>();

            if (holder == null) {
                throw new TelegramHolderNotInjectedException();
            }

            foreach (var bot in holder) {
                app.Map(bot.BasePath + bot.Endpoint, 
                    builder => builder.UseMiddleware<TelegramRouting>(bot.Endpoint));
                if (options is {UpdateWebhook: true}) {
                    if (options.WebhookUrl != null) {
                        var url = options.WebhookUrl;
                        logger?.LogDebug("Setting webhook at URL '{Url}'", Utils.ObfuscateToken(url));
                        bot.Bot.SetWebhookAsync(url).Start();
                    } else if (options.WebhookBaseUrl != null) {
                        var url = Url.Combine(options.WebhookBaseUrl, bot.BasePath, bot.Endpoint);
                        logger?.LogDebug("Setting webhook at URL '{Url}'", Utils.ObfuscateToken(url));
                        bot.Bot.SetWebhookAsync(url).Start();
                    }
                    else {
                        logger?.LogWarning("Missing both WebhookUrl and WebhookBaseUrl. Telegram's webhook update skipped");
                    }
                }
                bot.Dispatcher.SetServices(app.ApplicationServices);
                bot.Username = (bot.Bot.GetMeAsync().Result).Username;
            }
            return app;
        }
        
        public static IApplicationBuilder UseTelegramPolling(this IApplicationBuilder app) {
            var holder = app.ApplicationServices.GetService<ITelegramHolder>();
            var logger = app.ApplicationServices.GetService<ILogger<IApplicationBuilder>>();
            
            if (holder == null) {
                throw new TelegramHolderNotInjectedException();
            }

            foreach (var bot in holder) {
                bot.Dispatcher.SetServices(app.ApplicationServices);
            }
            foreach (var bot in holder) {
                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;
                var receiverOptions = new ReceiverOptions {
                    AllowedUpdates = {} // receive all update types
                };
                
                bot.Bot.DeleteWebhookAsync(cancellationToken: cancellationToken).Wait(cancellationToken);
                app.Map(bot.BasePath + bot.Endpoint, 
                    builder => builder.Run(async context => {
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("Ok", cancellationToken);
                    }));
                User me = bot.Bot.GetMeAsync(cancellationToken).Result;
                bot.Username = me.Username;
                bot.Bot.StartReceiving((client, update, cancelToken) => bot.Dispatcher.DispatchUpdateAsync(update, app.ApplicationServices),
                    (client, exception, cancelToken) => bot.Dispatcher.HandleErrorAsync(exception, app.ApplicationServices), 
                    receiverOptions, cancellationToken);
            }
            return app;
        }

        public static IApplicationBuilder UseStartupNewsletter(this IApplicationBuilder app) {
            using (var scoped = app.ApplicationServices.CreateScope()) {
                var holder = scoped.ServiceProvider.GetService<ITelegramHolder>();
                var logger = app.ApplicationServices.GetService<ILogger<IApplicationBuilder>>();
                
                if (holder == null) {
                    throw new TelegramHolderNotInjectedException();
                }
            
                var newsletterService = scoped.ServiceProvider.GetService<INewsletterService>();
                if (newsletterService == null) {
                    throw new NewsletterServiceNotInjectedException();
                }

                foreach (var botData in holder) {
                    if (botData.StartupNewsletter != null) {
                        if (newsletterService.GetNewsletterByKey(botData.StartupNewsletter.NewsletterKey) != null) {
                            logger?.LogInformation("Bot @{Username}: sending startup message to newsletter {NewsletterKey}",
                                botData.Username, botData.StartupNewsletter.NewsletterKey);
                            var result = newsletterService.SendNewsletter(botData.StartupNewsletter.NewsletterKey, chat => {
                                botData.StartupNewsletter.Action(botData, chat, app.ApplicationServices);
                            });
                            logger?.LogInformation("Sending completed. Result: Successes: {TotalSuccesses} - " +
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