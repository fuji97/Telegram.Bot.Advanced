using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace Telegram.Bot.Advanced.IntegrationTests.Infrastructure;

/// <summary>
/// Registered as a DI singleton (never static state) so <see cref="IntegrationProbeController"/> handler calls and
/// bot replies observed during a test can be asserted from outside the dispatcher's per-update scope.
/// </summary>
public sealed class ProbeRecorder {
    public ConcurrentQueue<string> HandlerCalls { get; } = new();
    public ConcurrentQueue<Message> SentMessages { get; } = new();
}
