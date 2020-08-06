using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Dispatcher.Filters;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;

namespace Telegram.Bot.Advanced.TestServer.TelegramController {
    public class TelegramPollingController : TelegramController<TestTelegramContext> {
        private readonly ILogger<TelegramPollingController> _logger;
        private readonly INewsletterService _newsletterService;

        public TelegramPollingController(ILogger<TelegramPollingController> logger, INewsletterService newsletterService) {
            _logger = logger;
            _newsletterService = newsletterService;
        }

        [CommandFilter("help")]
        public void Help() {
            BotData.Bot.SendTextMessageAsync(TelegramChat.Id, "Hello World!\nSiamo in polling mode.").Wait();
        }
        
        [CommandFilter("async")]
        public async Task AsyncMethod() {
            await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, "Hello World!\nSiamo in polling mode.");
            //_logger.LogInformation(result.Caption);
        }

        [CommandFilter("setup")]
        public async Task Setup() {
            TelegramChat.Role = ChatRole.Administrator;
            await _newsletterService.CreateNewsletterAsync(new Newsletter("default", "The default newsletter."));
            await TelegramContext.SaveChangesAsync();

            await ReplyTextMessageAsync("Done");
        }
    }
}