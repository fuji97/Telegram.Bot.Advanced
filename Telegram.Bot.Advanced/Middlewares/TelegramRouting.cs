using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Middlewares {
    class TelegramRouting {
        private readonly ITelegramBotClient bot;
        private readonly RequestDelegate next;
        private readonly IDictionary<string, ITelegramBotClient> botProvider;

        public TelegramRouting(RequestDelegate request, ITelegramBotClient bot, IServiceProvider serviceProvider) {
            this.next = request;
            this.bot = bot;
            
        }

        public async Task Invoke(HttpContext context) {

        }
    }
}
