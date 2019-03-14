using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Middlewares {
    public class TelegramRouting {
        private readonly string _endpoint;

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

            if (update == null) {
                logger.LogError("Can not parse webhook request into Update");
            }

            var botData = holder.Get(_endpoint);
            if (botData != null) {
                await botData.Dispatcher.DispatchUpdateAsync(update, provider);
            }

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("Ok");
        }
    }
}
