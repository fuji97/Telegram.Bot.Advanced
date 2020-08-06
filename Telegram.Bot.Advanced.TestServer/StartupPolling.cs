using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Dispatcher;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Advanced.TestServer.TelegramController;

namespace Telegram.Bot.Advanced.TestServer {
    public class StartupPolling {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Dispatcher<TestTelegramContext, TelegramPollingController>> _pollingLogger;

        public StartupPolling(IConfiguration configuration, ILogger<Dispatcher<TestTelegramContext, TelegramPollingController>> pollingLogger) {
            _configuration = configuration;
            _pollingLogger = pollingLogger;
        }
        
        public void ConfigureServices(IServiceCollection services) {
            services.AddEntityFrameworkInMemoryDatabase();
            services.AddDbContext<TestTelegramContext>();

            services.AddTelegramHolder(
                new TelegramBotData(options => {
                    options.CreateTelegramBotClient(_configuration["BotToken"]);
                    options.DispatcherBuilder = new DispatcherBuilder<TestTelegramContext, TelegramPollingController>()
                        .SetLogger(_pollingLogger)
                        .RegisterNewsletterController<TestTelegramContext>(services);
                    options.BasePath = _configuration["Telegram:Webhook"];
                })
            );

            services.AddNewsletter<TestTelegramContext>();

            services.AddMvc()
                .AddMvcOptions(options => options.EnableEndpointRouting = false)
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseTelegramPolling();
            app.UseMvc();
        }
    }
}