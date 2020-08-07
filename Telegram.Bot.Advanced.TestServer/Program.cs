using System;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
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
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        
            try
            {
                Log.Information("Starting up");
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) {
            var webhost = WebHost.CreateDefaultBuilder(args)
                .UseSerilog();
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