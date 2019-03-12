using Microsoft.AspNetCore.Builder;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Extensions {
    public static class ApplicationBuilderExtensions {
        public static IApplicationBuilder UseTelegramRouting<TContext, TController>(this IApplicationBuilder builder,
            string botKey, string basicPath = "/telegram", string webhookEndpoint = null)
            where TContext : TelegramContext, new()
            where TController : class, ITelegramController<TContext>, new() {
            if (webhookEndpoint == null)
                webhookEndpoint = botKey;

            return builder;
        }
    }
}