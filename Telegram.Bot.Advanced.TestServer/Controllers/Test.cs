using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.TestServer.Controllers {
    [ApiController]
    [Route("[controller]")]
    public class TestController : Microsoft.AspNetCore.Mvc.Controller {
        public ITelegramHolder holder;

        public TestController(ITelegramHolder holder) {
            this.holder = holder;
        }
        
        // GET
        [HttpGet("webhook")]
        public async Task<IActionResult> Index() {
            List<string> keys = new List<string>();
            foreach (var bot in holder) {
                await bot.Bot.SetWebhookAsync("https://link/proxy/telegram/test");
                keys.Add((await bot.Bot.GetWebhookInfoAsync()).Url);
            }
            return Ok(keys);
        }
    }
}