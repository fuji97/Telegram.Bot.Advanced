using System.Reflection;
using Microsoft.AspNetCore.Http;
using Telegram.Bot.Advanced.Core.Holder;

namespace Telegram.Bot.Advanced.Tests.Infrastructure;

/// <summary>
/// Reflection shim over the internal <c>Telegram.Bot.Advanced.Core.Middlewares.TelegramRouting.HandleAsync</c>
/// static method. The type is <c>internal</c> but the method itself is <c>public</c>; no InternalsVisibleTo and no
/// production changes are needed to call it directly and inspect its returned <see cref="IResult"/>.
/// </summary>
public static class TelegramRoutingInvoker {
    private static readonly MethodInfo HandleAsyncMethod = ResolveHandleAsyncMethod();

    public static Task<IResult> HandleAsync(HttpContext context, string endpoint) =>
        (Task<IResult>) HandleAsyncMethod.Invoke(null, [context, endpoint])!;

    private static MethodInfo ResolveHandleAsyncMethod() {
        var assembly = typeof(ITelegramBotData).Assembly;
        var type = assembly.GetType("Telegram.Bot.Advanced.Core.Middlewares.TelegramRouting", throwOnError: true)!;
        return type.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Static)
               ?? throw new MissingMethodException("TelegramRouting.HandleAsync was not found via reflection.");
    }
}
