using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;

namespace Telegram.Bot.Advanced.TestServer.TelegramController;

public sealed class TelegramWebhookController : TelegramController<TestTelegramContext> {
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(ILogger<TelegramWebhookController> logger) {
        _logger = logger;
    }

    [CommandFilter("help")]
    public async Task Help() {
        _logger.LogInformation("Hello World");
        await BotData.Bot.SendMessage(TelegramChat!.Id, "Hello World", cancellationToken: CancellationToken);
    }

    [CommandFilter("command")]
    public async Task Command() {
        foreach (var param in MessageCommand.Parameters) {
            await BotData.Bot.SendMessage(TelegramChat!.Id, param, cancellationToken: CancellationToken);
        }
    }

    [NoCommandFilter]
    public async Task General() {
        _logger.LogInformation("{Text}", MessageCommand.Text);
        await BotData.Bot.SendMessage(TelegramChat!.Id, MessageCommand.Text ?? string.Empty, cancellationToken: CancellationToken);
    }

    [CommandFilter("next"), DefaultChatStateFilter]
    public async Task NoState() {
        await BotData.Bot.SendMessage(TelegramChat!.Id, "Imposto stato uno", cancellationToken: CancellationToken);

        TelegramChat!["text"] = MessageCommand.Parameters.Count > 0 ? MessageCommand.Parameters[0] : null;
        TelegramChat.State = "1";

        await TelegramContext.SaveChangesAsync(CancellationToken);
    }

    [CommandFilter("next"), ChatStateFilter("1")]
    public async Task FirstState() {
        await BotData.Bot.SendMessage(TelegramChat!.Id, "Sei in stato uno, passi allo stato due", cancellationToken: CancellationToken);

        var text = TelegramChat!["text"];
        if (text != null) {
            await BotData.Bot.SendMessage(TelegramChat.Id, text, cancellationToken: CancellationToken);
        }

        if (MessageCommand.Parameters.Count > 0) {
            TelegramChat["text"] = MessageCommand.Parameters[0];
        }

        TelegramChat.State = "2";

        await TelegramContext.SaveChangesAsync(CancellationToken);
    }

    [CommandFilter("next"), ChatStateFilter("2")]
    public async Task SecondState() {
        await BotData.Bot.SendMessage(TelegramChat!.Id, "Sei in stato due, torni a senza stato", cancellationToken: CancellationToken);

        var text = TelegramChat!["text"];
        if (text != null) {
            await BotData.Bot.SendMessage(TelegramChat.Id, text, cancellationToken: CancellationToken);
        }

        TelegramChat.State = null;

        await TelegramContext.SaveChangesAsync(CancellationToken);
    }
}
