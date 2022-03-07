using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Middlewares {
    public class TelegramRouting {
        private readonly string _endpoint;
        private int? _lastUpdateId = null;

        public TelegramRouting(RequestDelegate request, string endpoint) {
            _endpoint = endpoint;
        }

        public async Task InvokeAsync(HttpContext context, ITelegramHolder holder, IServiceProvider provider,
            ILogger<TelegramRouting> logger) {
            
            Update update;
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(context.Request.Body))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                update = serializer.Deserialize<Update>(jsonTextReader);
            }

            if (update != null) {
                // Avoid multiple dispatching of the same update in case of delays
                if (_lastUpdateId.GetValueOrDefault(-1) != update.Id) {
                    var botData = holder.Get(_endpoint);
                    if (botData is not null) {
                        await botData.Dispatcher.DispatchUpdateAsync(update, provider);
                    }
                    else {
                        logger.LogWarning("No bots found that can manage this request");
                    }
                }
                else {
                    logger.LogInformation("Duplicate update received - Ignoring...");
                }
            }
            else {
                logger.LogError("Cannot parse webhook request into Update");
            }

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("Ok");
        }
    }
}
