using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;

namespace Telegram.Bot.Advanced.IntegrationTests.Infrastructure;

/// <summary>
/// The single controller driving every integration lane: echoes text, drives a two-step chat-state machine through
/// a real database, and exposes a handler that always throws to prove the pre-handler persistence and error
/// behavior of the real dispatcher/webhook pipeline.
/// </summary>
public sealed class IntegrationProbeController : TelegramController<IntegrationTelegramContext> {
    private readonly ProbeRecorder _recorder;

    public IntegrationProbeController(ProbeRecorder recorder) {
        _recorder = recorder;
    }

    [CommandFilter("echo")]
    public async Task Echo() {
        _recorder.HandlerCalls.Enqueue("echo");
        var message = await ReplyTextMessageAsync(
            "echo:" + string.Join(' ', MessageCommand.Parameters), cancellationToken: CancellationToken);
        _recorder.SentMessages.Enqueue(message);
    }

    [CommandFilter("state"), DefaultChatStateFilter]
    public async Task EnterState() {
        TelegramChat!["last"] = MessageCommand.Parameters.FirstOrDefault();
        TelegramChat.State = "probe";
        await TelegramContext.SaveChangesAsync(CancellationToken);
        _recorder.HandlerCalls.Enqueue("state:enter");
    }

    [CommandFilter("state"), ChatStateFilter("probe")]
    public async Task LeaveState() {
        _recorder.HandlerCalls.Enqueue("state:leave:" + TelegramChat!["last"]);
        TelegramChat.State = null;
        await TelegramContext.SaveChangesAsync(CancellationToken);
    }

    [CommandFilter("boom")]
    public Task Boom() => throw new InvalidOperationException("probe boom");

    [NoMethodFilter]
    public Task Fallback() {
        _recorder.HandlerCalls.Enqueue("fallback:" + (MessageCommand.Text ?? ""));
        return Task.CompletedTask;
    }
}
