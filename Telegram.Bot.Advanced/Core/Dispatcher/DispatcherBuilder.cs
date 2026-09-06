using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.DbContexts;
using Telegram.Bot.Advanced.Exceptions;

namespace Telegram.Bot.Advanced.Core.Dispatcher;

/// <summary>
/// Builder for a Dispatcher that resolves its controllers only from explicitly registered types.
/// </summary>
/// <typeparam name="TContext">Context used by the application</typeparam>
public sealed class DispatcherBuilder<TContext> : IDispatcherBuilder
    where TContext : TelegramContext {

    private readonly List<Type> _controllers = [];
    private ITelegramBotData? _botData;

    /// <inheritdoc />
    public IDispatcherBuilder SetTelegramBotData(ITelegramBotData botData) {
        _botData = botData;
        return this;
    }

    /// <inheritdoc />
    public IDispatcherBuilder AddControllers(params Type[] controllers) {
        _controllers.AddRange(controllers);
        return this;
    }

    /// <inheritdoc />
    public IDispatcher Build() {
        if (_botData == null) {
            throw new TelegramBotAdvancedException(
                "Cannot build a Dispatcher without ITelegramBotData. Call SetTelegramBotData() before calling Build().");
        }

        return new Dispatcher<TContext>(_botData, _controllers);
    }
}

/// <summary>
/// Builder for a Dispatcher that always registers <typeparamref name="TController"/> as its first controller,
/// in addition to any controller added through <see cref="AddControllers"/>.
/// </summary>
/// <typeparam name="TContext">Context used by the application</typeparam>
/// <typeparam name="TController">Controller prepended to the dispatcher's controller list</typeparam>
public sealed class DispatcherBuilder<TContext, TController> : IDispatcherBuilder
    where TContext : TelegramContext
    where TController : class, ITelegramController<TContext> {

    private readonly List<Type> _controllers = [];
    private ITelegramBotData? _botData;

    /// <inheritdoc />
    public IDispatcherBuilder SetTelegramBotData(ITelegramBotData botData) {
        _botData = botData;
        return this;
    }

    /// <inheritdoc />
    public IDispatcherBuilder AddControllers(params Type[] controllers) {
        _controllers.AddRange(controllers);
        return this;
    }

    /// <inheritdoc />
    public IDispatcher Build() {
        if (_botData == null) {
            throw new TelegramBotAdvancedException(
                "Cannot build a Dispatcher without ITelegramBotData. Call SetTelegramBotData() before calling Build().");
        }

        List<Type> controllers = [typeof(TController), .. _controllers];

        return new Dispatcher<TContext>(_botData, controllers);
    }
}
