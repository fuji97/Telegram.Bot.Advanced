using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Dispatcher;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Holder;
using Telegram.Bot.Advanced.TestServer.TelegramController;

namespace Telegram.Bot.Advanced.TestServer {
    public class Startup {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Dispatcher<TestTelegramContext, TelegramPollingController>> _pollingLogger;

        public Startup(IConfiguration configuration, ILogger<Dispatcher<TestTelegramContext, TelegramPollingController>> pollingLogger) {
            _configuration = configuration;
            _pollingLogger = pollingLogger;
        }
        
        public void ConfigureServices(IServiceCollection services) {
            services.AddEntityFrameworkInMemoryDatabase();
            services.AddDbContext<TestTelegramContext>();
            
            services.AddTelegramHolder(
                new TelegramBotDataBuilder()
                    .CreateTelegramBotClient(_configuration["TELEGRAM_BOT_KEY"])
                    .UseDispatcherBuilder(new DispatcherBuilder<TestTelegramContext, TelegramPollingController>().SetLogger(_pollingLogger))
                    .SetBasePath(_configuration["Telegram:Webhook"])
                    .Build()
                 );
            
            /*
            services.AddTelegramHolder(new TelegramBotData(new TelegramBotClient(_configuration["TELEGRAM_BOT_KEY"]),
                new DispatcherBuilder<TestTelegramContext, TelegramTestController>(),
                _configuration["TELEGRAM_BOT_KEY"],
                _configuration["Telegram:Webhook"]));
            */

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            //app.UseTelegramRouting();
            app.UseTelegramPolling();
            app.UseMvc();
        }
    }
}