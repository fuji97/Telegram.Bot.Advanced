using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Core.Hosting;

namespace Telegram.Bot.Advanced.Extensions;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddTelegramHolder(this IServiceCollection services, params ITelegramBotData[] bots) {
        services.AddSingleton<ITelegramHolder>(_ => new TelegramHolder(bots));

        // Register controllers to DI
        foreach (var bot in bots) {
            bot.Dispatcher.RegisterController(services);
        }

        return services;
    }

    /// <summary>
    /// Registers the long-polling transport hosted service. Exactly one of <see cref="AddTelegramPolling"/> or
    /// <see cref="AddTelegramWebhooks"/> may be registered.
    /// </summary>
    public static IServiceCollection AddTelegramPolling(this IServiceCollection services) {
        EnsureNoTransportRegistered(services);
        services.AddHostedService<TelegramPollingHostedService>();
        return services;
    }

    /// <summary>
    /// Registers the webhook transport hosted service, which registers each bot's webhook at startup. Exactly one
    /// of <see cref="AddTelegramPolling"/> or <see cref="AddTelegramWebhooks"/> may be registered. Call
    /// <see cref="ApplicationBuilderExtensions.MapTelegramWebhooks"/> to expose the webhook endpoints.
    /// </summary>
    public static IServiceCollection AddTelegramWebhooks(this IServiceCollection services, Action<TelegramWebhookOptions> configureOptions) {
        ArgumentNullException.ThrowIfNull(configureOptions);

        EnsureNoTransportRegistered(services);
        services.Configure(configureOptions);
        services.AddHostedService<TelegramWebhookHostedService>();
        return services;
    }

    /// <summary>
    /// Registers the hosted service that sends each bot's configured <see cref="Models.StartupNewsletter"/> once
    /// the transport is up. Register after the selected transport.
    /// </summary>
    public static IServiceCollection AddStartupNewsletter(this IServiceCollection services) {
        services.AddHostedService<StartupNewsletterHostedService>();
        return services;
    }

    private static void EnsureNoTransportRegistered(IServiceCollection services) {
        var alreadyRegistered = services.Any(descriptor =>
            descriptor.ServiceType == typeof(IHostedService) &&
            (descriptor.ImplementationType == typeof(TelegramPollingHostedService) ||
             descriptor.ImplementationType == typeof(TelegramWebhookHostedService)));

        if (alreadyRegistered) {
            throw new InvalidOperationException(
                "A Telegram transport is already registered. Call either AddTelegramPolling or AddTelegramWebhooks exactly once.");
        }
    }
}
