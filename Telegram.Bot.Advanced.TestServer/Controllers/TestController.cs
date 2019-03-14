using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.TestServer.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class TestController : Microsoft.AspNetCore.Mvc.Controller {
        public readonly ITelegramHolder Holder;

        public TestController(ITelegramHolder holder) {
            Holder = holder;
        }
        
        // GET
        [HttpGet("webhook")]
        public async Task<IActionResult> Index() {
            List<string> keys = new List<string>();
            foreach (var bot in Holder) {
                await bot.Bot.SetWebhookAsync("https://link/proxy/telegram/test");
                keys.Add((await bot.Bot.GetWebhookInfoAsync()).Url);
            }
            return Ok(keys);
        }
    }
}