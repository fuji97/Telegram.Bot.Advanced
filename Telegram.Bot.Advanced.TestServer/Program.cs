using Microsoft.EntityFrameworkCore;
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

var startupType = args.Contains(StartupTypeConst.Webhook)
    ? StartupType.Webhook
    : StartupType.Polling;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

var services = builder.Services;
var configuration = builder.Configuration;

services.AddDbContext<TestTelegramContext>(options => options.UseInMemoryDatabase("database_test"));

var botToken = configuration["BotToken"] ?? throw new InvalidOperationException("BotToken configuration value is required.");
var basePath = configuration["Telegram:Webhook"] ?? throw new InvalidOperationException("Telegram:Webhook configuration value is required.");

services.AddTelegramHolder(
    new TelegramBotData(options => {
        options.CreateTelegramBotClient(botToken);
        options.Endpoint = "test";

        options.DispatcherBuilder = startupType switch {
            StartupType.Webhook => new DispatcherBuilder<TestTelegramContext, TelegramWebhookController>()
                .RegisterNewsletterController<TestTelegramContext>(),
            StartupType.Polling => new DispatcherBuilder<TestTelegramContext, TelegramPollingController>()
                .RegisterNewsletterController<TestTelegramContext>(),
            _ => throw new InvalidOperationException($"Unsupported startup type '{startupType}'.")
        };

        options.BasePath = basePath;

        options.DefaultUserRole.Add(
            new UserRole("fuji97", ChatRole.Administrator)
        );

        options.GroupChatBehaviour = IgnoreBehaviour.IgnoreNothing;
        options.UserUpdate = UserUpdate.EveryMessage;

        options.StartupNewsletter = new StartupNewsletter("startup", async (data, chat, sp, cancellationToken) => {
            var logger = sp.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Sending startup message to {Username}", chat.Username);
            await data.Bot.SendMessage(chat.Id, $"The bot @{data.Username} is now online!", cancellationToken: cancellationToken);
        });
    })
);

services.AddNewsletter<TestTelegramContext>();

switch (startupType) {
    case StartupType.Polling:
        services.AddTelegramPolling();
        break;
    case StartupType.Webhook:
        services.AddTelegramWebhooks(options => {
            options.BaseUri = new Uri(configuration["Telegram:BaseUrl"] ?? throw new InvalidOperationException("Telegram:BaseUrl configuration value is required."));
            options.SecretToken = configuration["Telegram:WebhookSecret"] ?? throw new InvalidOperationException("Telegram:WebhookSecret configuration value is required. Set it through user secrets or an environment variable.");
        });
        break;
}

services.AddStartupNewsletter();

// Build App
var app = builder.Build();
var logger = app.Logger;

// Seed database before any hosted service (including the startup newsletter) runs.
await app.SeedDataAsync();

logger.LogInformation("Starting in {StartupType} mode...", startupType);

if (startupType == StartupType.Webhook) {
    app.MapTelegramWebhooks();
}

await app.RunAsync();
