using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.DbContexts;

namespace Telegram.Bot.Advanced.Controller;

public static class NewsletterControllerExtensions {
    public static IDispatcherBuilder RegisterNewsletterController<TContext>(
        this IDispatcherBuilder dispatcherBuilder) where TContext : TelegramContext {

        dispatcherBuilder.AddControllers(
            typeof(NewsletterSubscriptionController<TContext>),
            typeof(NewsletterPublishingController<TContext>));

        return dispatcherBuilder;
    }
}
