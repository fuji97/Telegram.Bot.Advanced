using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.Core.Middlewares;

/// <summary>
/// Handles a single incoming Telegram webhook request for a specific bot endpoint: authenticates the secret
/// token in fixed time, deserializes the update, and dispatches it. Mapped per-bot by
/// <see cref="ApplicationBuilderExtensions.MapTelegramWebhooks"/>.
/// </summary>
internal static class TelegramRouting {
    private const string SecretTokenHeader = "X-Telegram-Bot-Api-Secret-Token";
    private const string LoggerCategory = "Telegram.Bot.Advanced.Core.Middlewares.TelegramRouting";

    public static async Task<IResult> HandleAsync(HttpContext context, string endpoint) {
        var options = context.RequestServices.GetRequiredService<IOptions<TelegramWebhookOptions>>().Value;

        if (!IsSecretValid(context, options.SecretToken)) {
            return Results.Unauthorized();
        }

        var holder = context.RequestServices.GetRequiredService<ITelegramHolder>();
        if (!holder.TryGet(endpoint, out var botData)) {
            CreateLogger(context).LogWarning("No bot registered for endpoint '{Endpoint}'", endpoint);
            return Results.NotFound();
        }

        Update? update;
        try {
            update = await JsonSerializer.DeserializeAsync<Update>(context.Request.Body, JsonBotAPI.Options, context.RequestAborted);
        }
        catch (JsonException) {
            CreateLogger(context).LogWarning("Cannot parse webhook request body into an Update for endpoint '{Endpoint}'", endpoint);
            return Results.BadRequest();
        }

        if (update is null) {
            CreateLogger(context).LogWarning("Webhook request body deserialized to a null Update for endpoint '{Endpoint}'", endpoint);
            return Results.BadRequest();
        }

        // Dispatch exceptions are allowed to propagate as HTTP 500 so Telegram retries the delivery.
        await botData.Dispatcher.DispatchUpdateAsync(update, context.RequestServices, context.RequestAborted);

        return Results.Ok("Ok");
    }

    private static ILogger CreateLogger(HttpContext context) {
        return context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategory);
    }

    private static bool IsSecretValid(HttpContext context, string? secretToken) {
        if (string.IsNullOrEmpty(secretToken)) {
            return false;
        }

        var header = context.Request.Headers[SecretTokenHeader];
        if (header.Count != 1 || string.IsNullOrEmpty(header[0])) {
            return false;
        }

        var headerValue = header[0]!;

        var expectedByteCount = Encoding.UTF8.GetByteCount(secretToken);
        var actualByteCount = Encoding.UTF8.GetByteCount(headerValue);

        // Lengths are not sensitive; only the byte content comparison below needs to run in fixed time.
        if (expectedByteCount != actualByteCount) {
            return false;
        }

        Span<byte> expected = expectedByteCount <= 256 ? stackalloc byte[expectedByteCount] : new byte[expectedByteCount];
        Span<byte> actual = actualByteCount <= 256 ? stackalloc byte[actualByteCount] : new byte[actualByteCount];

        Encoding.UTF8.GetBytes(secretToken, expected);
        Encoding.UTF8.GetBytes(headerValue, actual);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
