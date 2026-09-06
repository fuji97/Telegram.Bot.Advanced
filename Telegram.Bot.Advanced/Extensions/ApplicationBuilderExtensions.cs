using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Core.Middlewares;

namespace Telegram.Bot.Advanced.Extensions;

public static class ApplicationBuilderExtensions {
    /// <summary>
    /// Maps a POST-only webhook endpoint for every bot registered in <see cref="ITelegramHolder"/>, at
    /// "{BasePath}{Endpoint}". Requires <see cref="ServiceCollectionExtensions.AddTelegramWebhooks"/> to have been
    /// called.
    /// </summary>
    public static IEndpointRouteBuilder MapTelegramWebhooks(this IEndpointRouteBuilder endpoints) {
        var holder = endpoints.ServiceProvider.GetRequiredService<ITelegramHolder>();

        foreach (var bot in holder) {
            var route = $"{bot.BasePath}{bot.Endpoint}";
            endpoints.MapPost(route, (Delegate) ((HttpContext context) => TelegramRouting.HandleAsync(context, bot.Endpoint)));
        }

        return endpoints;
    }
}
