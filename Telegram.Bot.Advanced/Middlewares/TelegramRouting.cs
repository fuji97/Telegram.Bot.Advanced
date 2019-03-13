using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace Telegram.Bot.Advanced.Middlewares {
    class TelegramRouting {
        private readonly RequestDelegate next;
        private readonly string endpoint;

        public TelegramRouting(RequestDelegate request, string endpoint) {
            this.next = request;
            this.endpoint = endpoint;
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

            var botData = holder.Get(endpoint);
            if (botData != null) {
                await botData.Dispatcher.DispatchUpdateAsync(update, provider);
            }

            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("Ok");
        }
    }
}
