using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.TestServer.Models;
using Telegram.Bot.Advanced.TestServer.TelegramController;

namespace Telegram.Bot.Advanced.TestServer {
    public class Program {
        private static StartupType _startupType = StartupType.Polling;
        public static void Main(string[] args) {
            var mode = args.FirstOrDefault(
                x => x == StartupTypeConst.Polling || 
                     x == StartupTypeConst.Webhook);

            if (mode == StartupTypeConst.Webhook) {
                _startupType = StartupType.Webhook;
            }
            if (mode == StartupTypeConst.Polling) {
                _startupType = StartupType.Polling;
            }

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            var webhost = WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) => {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                });
            switch (_startupType) {
                case StartupType.Polling:
                    webhost.UseStartup<StartupPolling>();
                    break;
                case StartupType.Webhook:
                    webhost.UseStartup<StartupWebhook>();
                    break;
            }

            return webhost;
        }
    }
}