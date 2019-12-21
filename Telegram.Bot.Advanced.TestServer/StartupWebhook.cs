using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Dispatcher;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.TestServer.TelegramController;

namespace Telegram.Bot.Advanced.TestServer {
    public class StartupWebhook {
        private readonly IConfiguration _configuration;

        public StartupWebhook(IConfiguration configuration) {
            _configuration = configuration;
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

            services.AddMvc()
                .AddMvcOptions(options => options.EnableEndpointRouting = false)
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseTelegramRouting(new TelegramRoutingOptions() {
                WebhookBaseUrl = _configuration["BaseUrl"]
            });
            app.UseMvc();
        }
    }
}