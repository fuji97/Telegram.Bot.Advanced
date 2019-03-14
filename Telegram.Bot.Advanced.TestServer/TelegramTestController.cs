using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DispatcherFilters;

namespace Telegram.Bot.Advanced.TestServer {
    public class TelegramTestController : TelegramController<TestTelegramContext> {
        private readonly ILogger<TelegramTestController> _logger;
        private readonly TestTelegramContext _context;

        public TelegramTestController() {}
        
        public TelegramTestController(ILogger<TelegramTestController> logger, TestTelegramContext context) {
            _logger = logger;
            _context = context;
        }

        [CommandFilter("help")]
        public async Task Help() {
            _logger.LogInformation("Hello World");
            await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, "Hello World");
        }
    }
}