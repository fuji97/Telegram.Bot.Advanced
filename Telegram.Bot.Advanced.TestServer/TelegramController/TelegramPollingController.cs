using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;

namespace Telegram.Bot.Advanced.TestServer.TelegramController;

public sealed class TelegramPollingController : TelegramController<TestTelegramContext> {
    private readonly ILogger<TelegramPollingController> _logger;
    private readonly INewsletterService _newsletterService;

    public TelegramPollingController(ILogger<TelegramPollingController> logger, INewsletterService newsletterService) {
        _logger = logger;
        _newsletterService = newsletterService;
    }

    [CommandFilter("help")]
    public async Task Help() {
        await BotData.Bot.SendMessage(TelegramChat!.Id, "Hello World!\nSiamo in polling mode.", cancellationToken: CancellationToken);
    }

    [CommandFilter("async")]
    public async Task AsyncMethod() {
        await BotData.Bot.SendMessage(TelegramChat!.Id, "Hello World!\nSiamo in polling mode.", cancellationToken: CancellationToken);
    }

    [CommandFilter("setup")]
    public async Task Setup() {
        TelegramChat!.Role = ChatRole.Administrator;
        await _newsletterService.CreateNewsletterAsync(new Newsletter("default", "The default newsletter."), CancellationToken);
        await TelegramContext.SaveChangesAsync(CancellationToken);

        await ReplyTextMessageAsync("Done", cancellationToken: CancellationToken);
    }
}
