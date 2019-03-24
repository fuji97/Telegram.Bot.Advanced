using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Dispatcher;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;

namespace Telegram.Bot.Advanced.TestServer.TelegramController {
    public class StartupWebhook {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Dispatcher<TestTelegramContext, TelegramPollingController>> _pollingLogger;

        public StartupWebhook(IConfiguration configuration, ILogger<Dispatcher<TestTelegramContext, TelegramPollingController>> pollingLogger) {
            _configuration = configuration;
            _pollingLogger = pollingLogger;
        }
        
        public void ConfigureServices(IServiceCollection services) {
            services.AddEntityFrameworkInMemoryDatabase();
            services.AddDbContext<TestTelegramContext>();
            
            services.AddTelegramHolder(
                new TelegramBotDataBuilder()
                    .CreateTelegramBotClient(_configuration["TELEGRAM_BOT_KEY"])
                    .UseDispatcherBuilder(new DispatcherBuilder<TestTelegramContext, TelegramWebhookController>())
                    .SetBasePath(_configuration["Telegram:Webhook"])
                    .Build()
            );

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseTelegramRouting();
            app.UseMvc();
        }
    }
}