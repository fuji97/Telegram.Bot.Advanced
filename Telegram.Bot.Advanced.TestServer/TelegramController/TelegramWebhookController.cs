using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;

namespace Telegram.Bot.Advanced.TestServer.TelegramController {
    public class TelegramWebhookController : TelegramController<TestTelegramContext> {
        private readonly ILogger<TelegramWebhookController> _logger;

        public TelegramWebhookController(ILogger<TelegramWebhookController> logger) {
            _logger = logger;
        }

        [CommandFilter("help")]
        public async Task Help() {
            _logger.LogInformation("Hello World");
            await BotData.Bot.SendMessage(TelegramChat!.Id, "Hello World");
        }
        
        [CommandFilter("command")]
        public async Task Command() {
            foreach (var param in MessageCommand.Parameters) {
                await BotData.Bot.SendMessage(TelegramChat!.Id, param);
            }
        }
        
        [NoCommandFilter]
        public async Task General() {
            _logger.LogInformation($"{MessageCommand.Text}");
            await BotData.Bot.SendMessage(TelegramChat!.Id, $"{MessageCommand.Text}");
        }

        [CommandFilter("next"), DefaultChatStateFilter]
        public async Task NoState() {
            await BotData.Bot.SendMessage(TelegramChat!.Id, "Imposto stato uno");
            if (MessageCommand.Parameters.Count > 0) {
                TelegramChat!["text"] = MessageCommand.Parameters[0];
            }
            else {
                TelegramChat!["text"] = null;
            }
            TelegramChat!.State = "1";
        }
        
        [CommandFilter("next"), ChatStateFilter("1")]
        public async Task FirstState() {
            await BotData.Bot.SendMessage(TelegramChat!.Id, "Sei in stato uno, passi allo stato due");
            var text = TelegramChat!["text"];
            if (text != null) {
                await BotData.Bot.SendMessage(TelegramChat.Id, text);
            }
            
            if (MessageCommand.Parameters.Count > 0) {
                TelegramChat["text"] = MessageCommand.Parameters[0];
            }
            
            TelegramChat.State = "2";
        }
        
        [CommandFilter("next"), ChatStateFilter("2")]
        public async Task SecondState() {
            await BotData.Bot.SendMessage(TelegramChat!.Id, "Sei in stato due, torni a senza stato");
            var text = TelegramChat!["text"];
            if (text != null) {
                await BotData.Bot.SendMessage(TelegramChat.Id, text);
            }
            TelegramChat.State = null;
        }
    }
}