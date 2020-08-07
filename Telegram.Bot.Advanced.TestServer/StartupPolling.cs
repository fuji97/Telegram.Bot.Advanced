using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Advanced.TestServer.SeedData;
using Telegram.Bot.Advanced.TestServer.TelegramController;

namespace Telegram.Bot.Advanced.TestServer {
    public class StartupPolling {
        private readonly IConfiguration _configuration;
        private ILogger<StartupPolling> _logger;

        public StartupPolling(IConfiguration configuration, ILogger<StartupPolling> logger) {
            _configuration = configuration;
            _logger = logger;
        }

        public void ConfigureServices(IServiceCollection services) {
            services.AddEntityFrameworkInMemoryDatabase();
            services.AddDbContext<TestTelegramContext>();

            services.AddTelegramHolder(
                new TelegramBotData(options => {
                    options.CreateTelegramBotClient(_configuration["BotToken"]);
                    
                    options.DispatcherBuilder = new DispatcherBuilder<TestTelegramContext, TelegramPollingController>()
                        //.SetLogger(_pollingLogger)
                        .RegisterNewsletterController<TestTelegramContext>();
                    
                    options.BasePath = _configuration["Telegram:Webhook"];
                    
                    options.DefaultUserRole.Add(
                        new UserRole("fuji97", ChatRole.Administrator)
                        );
                    
                    options.GroupChatBehaviour = IgnoreBehaviour.IgnoreNothing;
                    options.UserUpdate = UserUpdate.EveryMessage;
                    
                    options.StartupNewsletter = new StartupNewsletter("startup", (data, chat, sp) => {
                        var logger = sp.GetService<ILogger>();
                        logger?.LogInformation("Sending startup message to {Username}", chat.Username);
                        data.Bot.SendTextMessageAsync(chat.Id, $"The bot @{data.Username} is now online!");
                    });
                })
            );

            services.AddNewsletter<TestTelegramContext>();

            services.AddMvc()
                .AddMvcOptions(options => options.EnableEndpointRouting = false)
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            // Startup newsletter
            app.UseStartupNewsletter();
            
            // Seed database
            app.SeedData();
            
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseTelegramPolling();
            app.UseMvc();
        }
    }
}