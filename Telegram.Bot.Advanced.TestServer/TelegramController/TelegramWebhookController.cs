using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.DispatcherFilters;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Types;

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
        
        [CommandFilter("command")]
        public async Task Command() {
            foreach (var param in MessageCommand.Parameters) {
                await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, param);
            }
        }
        
        [NoCommandFilter]
        public async Task General() {
            _logger.LogInformation($"{MessageCommand.Text}");
            await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, $"{MessageCommand.Text}");
        }

        [CommandFilter("next"), ChatStateFilter(0)]
        public async Task NoState() {
            await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, "Imposto stato uno");
            if (MessageCommand.Parameters.Count > 0) {
                TelegramChat["text"] = MessageCommand.Parameters[0];
            }
            else {
                TelegramChat["text"] = null;
            }
            TelegramChat.State = 1;
        }
        
        [CommandFilter("next"), ChatStateFilter(1)]
        public async Task FirstState() {
            await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, "Sei in stato uno, passi allo stato due");
            if (TelegramChat["text"] != null) {
                await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, TelegramChat["text"]);
            }
            
            if (MessageCommand.Parameters.Count > 0) {
                TelegramChat["text"] = MessageCommand.Parameters[0];
            }
            
            TelegramChat.State = 2;
        }
        
        [CommandFilter("next"), ChatStateFilter(2)]
        public async Task SecondState() {
            await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, "Sei in stato due, torni a senza stato");
            if (TelegramChat["text"] != null) {
                await BotData.Bot.SendTextMessageAsync(TelegramChat.Id, TelegramChat["text"]);
            }
            TelegramChat.State = 0;
        }
    }
}