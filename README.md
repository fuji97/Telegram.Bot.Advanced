# Telegram.Bot.Advanced
![branch 'master' build](https://img.shields.io/appveyor/ci/fuji97/telegram-bot-advanced/master.svg?label=branch%20%27master%27%20build)
![branch 'develop' build](https://img.shields.io/appveyor/ci/fuji97/telegram-bot-advanced/develop.svg?label=branch%20%27develop%27%20build)
![nuget version](https://img.shields.io/nuget/v/Telegram.Bot.Advanced.svg)
![nuget download](https://img.shields.io/nuget/dt/Telegram.Bot.Advanced.svg)
![license](https://img.shields.io/github/license/fuji97/telegram.bot.advanced.svg)

This framework extends the .NET client [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) 

# Usage
### Add TelegramHolder to services
Add to your ConfigureServices
```
services.AddDbContext<TestTelegramContext>();

services.AddTelegramHolder(
    new TelegramBotDataBuilder()
        .CreateTelegramBotClient(_configuration["TELEGRAM_BOT_KEY"])
        .UseDispatcherBuilder(new DispatcherBuilder<TestTelegramContext, TelegramWebhookController>())
        .SetBasePath(_configuration["Telegram:Webhook"])
        .Build()
);
```