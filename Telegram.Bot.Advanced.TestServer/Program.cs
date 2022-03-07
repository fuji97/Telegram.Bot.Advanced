using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Extensions;
using Telegram.Bot.Advanced.Models;
using Telegram.Bot.Advanced.Services;
using Telegram.Bot.Advanced.TestServer;
using Telegram.Bot.Advanced.TestServer.Models;
using Telegram.Bot.Advanced.TestServer.SeedData;
using Telegram.Bot.Advanced.TestServer.TelegramController;

var startupType = StartupType.Polling;

var mode = args.FirstOrDefault(
    x => x == StartupTypeConst.Polling || 
         x == StartupTypeConst.Webhook);

if (mode == StartupTypeConst.Webhook) {
    startupType = StartupType.Webhook;
}
if (mode == StartupTypeConst.Polling) {
    startupType = StartupType.Polling;
}

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSerilog();
builder.Configuration.AddUserSecrets<Program>();

var services = builder.Services;
var configuration = builder.Configuration;

services.AddEntityFrameworkInMemoryDatabase();
services.AddDbContext<TestTelegramContext>();

services.AddTelegramHolder(
    new TelegramBotData(options => {
        options.CreateTelegramBotClient(configuration["BotToken"]);
                    
        options.DispatcherBuilder = new DispatcherBuilder<TestTelegramContext, TelegramPollingController>()
            .RegisterNewsletterController<TestTelegramContext>();
                    
        options.BasePath = configuration["Telegram:Webhook"];
                    
        options.DefaultUserRole.Add(
            new UserRole("fuji97", ChatRole.Administrator)
        );
                    
        options.GroupChatBehaviour = IgnoreBehaviour.IgnoreNothing;
        options.UserUpdate = UserUpdate.EveryMessage;
                    
        options.StartupNewsletter = new StartupNewsletter("startup", (data, chat, sp) => {
            var logger = sp.GetService<ILogger<Program>>();
            logger?.LogInformation("Sending startup message to {Username}", chat.Username);
            data.Bot.SendTextMessageAsync(chat.Id, $"The bot @{data.Username} is now online!");
        });
    })
);

services.AddNewsletter<TestTelegramContext>();

builder.Services.AddControllersWithViews();

// Build App
var app = builder.Build();
var logger = app.Logger;

app.UseStartupNewsletter();
            
// Seed database
app.SeedData();
            
if (app.Environment.IsDevelopment()) {
    app.UseDeveloperExceptionPage();
}

switch (startupType) {
    case StartupType.Polling:
        logger.LogInformation("Starting in polling mode...");
        app.UseTelegramPolling();
        break;
    case StartupType.Webhook:
        logger.LogInformation("Starting in webhook mode...");
        app.UseTelegramRouting(new TelegramRoutingOptions() {
            WebhookBaseUrl = configuration["BaseUrl"]
        });
        break;
}
app.UseRouting();

app.Run();