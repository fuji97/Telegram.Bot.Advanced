using System.Net;
using System.Text;
using System.Text.Json;
using Telegram.Bot;

namespace Telegram.Bot.Advanced.Tests.Infrastructure;

/// <summary>
/// Fake transport for every real <see cref="TelegramBotClient"/> used in the suite. Responses are queued per Bot
/// API method name (the last URI segment, e.g. "getMe", "sendMessage"); a method with no queued response and no
/// default response fails the test instead of ever touching the network.
/// </summary>
public sealed class TestHttpMessageHandler : HttpMessageHandler {
    public sealed record RecordedRequest(string Method, string? Body);

    private readonly Lock _gate = new();
    private readonly Dictionary<string, Queue<Func<HttpRequestMessage, string>>> _queued = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Func<HttpRequestMessage, string>> _defaults = new(StringComparer.Ordinal);
    private readonly List<RecordedRequest> _requests = [];

    public IReadOnlyList<RecordedRequest> Requests {
        get { lock (_gate) return [.. _requests]; }
    }

    public int CallCount(string method) {
        lock (_gate) return _requests.Count(r => string.Equals(r.Method, method, StringComparison.Ordinal));
    }

    /// <summary>
    /// Queues a single canned "{"ok":true,"result":...}" envelope for the next call to <paramref name="method"/>.
    /// </summary>
    public void Enqueue<TResult>(string method, TResult result) => EnqueueRaw(method, _ => Envelope(result));

    /// <summary>
    /// Sets the response used for every call to <paramref name="method"/> once its queue is empty. Needed for
    /// endpoints that are called an indeterminate number of times (e.g. a polling receive loop's "getUpdates").
    /// </summary>
    public void SetDefaultResponse<TResult>(string method, TResult result) {
        lock (_gate) _defaults[method] = _ => Envelope(result);
    }

    private void EnqueueRaw(string method, Func<HttpRequestMessage, string> factory) {
        lock (_gate) {
            if (!_queued.TryGetValue(method, out var queue)) {
                queue = new Queue<Func<HttpRequestMessage, string>>();
                _queued[method] = queue;
            }

            queue.Enqueue(factory);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        var method = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        Func<HttpRequestMessage, string>? factory;
        lock (_gate) {
            _requests.Add(new RecordedRequest(method, body));

            if (_queued.TryGetValue(method, out var queue) && queue.Count > 0) {
                factory = queue.Dequeue();
            }
            else {
                _defaults.TryGetValue(method, out factory);
            }
        }

        if (factory is null) {
            throw new InvalidOperationException(
                $"Unexpected Telegram Bot API call to method '{method}'. This test double never performs real " +
                "network I/O; queue a response for this method or set a default response before exercising it.");
        }

        return new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(factory(request), Encoding.UTF8, "application/json")
        };
    }

    private static string Envelope<TResult>(TResult result) =>
        JsonSerializer.Serialize(new { ok = true, result }, JsonBotAPI.Options);
}

/// <summary>
/// Builds a real <see cref="TelegramBotClient"/> backed by a fresh <see cref="TestHttpMessageHandler"/>. Every
/// <c>ITelegramBotData.Bot</c>/<c>ITelegramBotClient</c> call in the suite goes through this real client.
/// </summary>
public static class TestBotClientFactory {
    public static (TelegramBotClient Client, TestHttpMessageHandler Handler) Create() {
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var client = new TelegramBotClient(new TelegramBotClientOptions("123456789:TEST-TOKEN"), httpClient);
        return (client, handler);
    }
}
