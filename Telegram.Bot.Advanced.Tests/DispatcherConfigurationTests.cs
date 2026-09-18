using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Telegram.Bot.Advanced.Controller;
using Telegram.Bot.Advanced.Core.Dispatcher;
using Telegram.Bot.Advanced.Core.Dispatcher.Filters;
using Telegram.Bot.Advanced.Core.Holder;
using Telegram.Bot.Advanced.Exceptions;
using Telegram.Bot.Advanced.Tests.Infrastructure;

namespace Telegram.Bot.Advanced.Tests;

/// <summary>
/// Covers the construction/registration contract of <see cref="Dispatcher{TContext}"/> and both
/// <see cref="DispatcherBuilder{TContext}"/>/<see cref="DispatcherBuilder{TContext,TController}"/> shapes: controller
/// validation, handler-shape validation, fallback uniqueness, and builder wiring. Handler selection/dispatch
/// behaviour itself is covered in DispatcherRoutingTests.
/// </summary>
public sealed class DispatcherConfigurationTests {
    // ---- fixture controllers -------------------------------------------------------------------------------

    private sealed class ValidController : TelegramController<TestTelegramContext> {
        [CommandFilter("a")]
        public void A() { }
    }

    private sealed class SecondController : TelegramController<TestTelegramContext> {
        [CommandFilter("a")]
        public void A() { }
    }

    private sealed class ParameterizedController : TelegramController<TestTelegramContext> {
        public Task P(int x) => Task.CompletedTask;
    }

    private sealed class TwoFallbackController : TelegramController<TestTelegramContext> {
        [NoMethodFilter]
        public Task F1() => Task.CompletedTask;

        [NoMethodFilter]
        public Task F2() => Task.CompletedTask;
    }

    private abstract class AbstractController : TelegramController<TestTelegramContext> {
    }

    private sealed class NotAController {
    }

    // ---- constructor validation -----------------------------------------------------------------------------

    [Fact]
    public void Constructor_NullBotData_ThrowsArgumentNullException() {
        Assert.Throws<ArgumentNullException>(() => new Dispatcher<TestTelegramContext>(null!, [typeof(ValidController)]));
    }

    [Fact]
    public void Constructor_NullControllers_ThrowsArgumentNullException() {
        var botData = Substitute.For<ITelegramBotData>();

        Assert.Throws<ArgumentNullException>(() => new Dispatcher<TestTelegramContext>(botData, null!));
    }

    [Fact]
    public void Constructor_DuplicateControllerType_ThrowsInvalidControllerException() {
        var botData = Substitute.For<ITelegramBotData>();

        var exception = Assert.Throws<InvalidControllerException>(() =>
            new Dispatcher<TestTelegramContext>(botData, [typeof(ValidController), typeof(ValidController)]));

        Assert.Contains(typeof(ValidController).FullName!, exception.Message);
    }

    [Fact]
    public void Constructor_OpenGenericController_ThrowsInvalidControllerException() {
        var botData = Substitute.For<ITelegramBotData>();

        Assert.Throws<InvalidControllerException>(() =>
            new Dispatcher<TestTelegramContext>(botData, [typeof(NewsletterSubscriptionController<>)]));
    }

    [Fact]
    public void Constructor_TypeNotImplementingITelegramController_ThrowsInvalidControllerException() {
        var botData = Substitute.For<ITelegramBotData>();

        Assert.Throws<InvalidControllerException>(() =>
            new Dispatcher<TestTelegramContext>(botData, [typeof(NotAController)]));
    }

    [Fact]
    public void Constructor_AbstractController_ThrowsInvalidControllerException() {
        var botData = Substitute.For<ITelegramBotData>();

        Assert.Throws<InvalidControllerException>(() =>
            new Dispatcher<TestTelegramContext>(botData, [typeof(AbstractController)]));
    }

    [Fact]
    public void Constructor_HandlerWithParameters_ThrowsInvalidControllerException() {
        var botData = Substitute.For<ITelegramBotData>();

        Assert.Throws<InvalidControllerException>(() =>
            new Dispatcher<TestTelegramContext>(botData, [typeof(ParameterizedController)]));
    }

    [Fact]
    public void Constructor_TwoNoMethodFilterHandlers_ThrowsInvalidControllerException() {
        var botData = Substitute.For<ITelegramBotData>();

        var exception = Assert.Throws<InvalidControllerException>(() =>
            new Dispatcher<TestTelegramContext>(botData, [typeof(TwoFallbackController)]));

        Assert.Equal("Only one handler method may be marked with [NoMethodFilter].", exception.Message);
    }

    // ---- accessors / registration ----------------------------------------------------------------------------

    [Fact]
    public void GetContextType_ReturnsConfiguredContextType() {
        var botData = Substitute.For<ITelegramBotData>();
        var dispatcher = new Dispatcher<TestTelegramContext>(botData, [typeof(ValidController)]);

        Assert.Equal(typeof(TestTelegramContext), dispatcher.GetContextType());
    }

    [Fact]
    public void GetControllersType_ReturnsRegisteredTypes() {
        var botData = Substitute.For<ITelegramBotData>();
        IList<Type> controllers = [typeof(ValidController), typeof(SecondController)];
        var dispatcher = new Dispatcher<TestTelegramContext>(botData, controllers);

        Assert.Equal(controllers, dispatcher.GetControllersType());
    }

    [Fact]
    public void RegisterController_RegistersEachControllerAsScopedOnce() {
        var botData = Substitute.For<ITelegramBotData>();
        var dispatcher = new Dispatcher<TestTelegramContext>(botData, [typeof(ValidController)]);
        var services = new ServiceCollection();

        dispatcher.RegisterController(services);
        dispatcher.RegisterController(services);

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ValidController));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public async Task HandleErrorAsync_CancelledToken_ThrowsOperationCanceledException() {
        var botData = Substitute.For<ITelegramBotData>();
        var dispatcher = new Dispatcher<TestTelegramContext>(botData, []);
        var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.HandleErrorAsync(new InvalidOperationException("boom"), provider, cts.Token));
    }

    [Fact]
    public async Task HandleErrorAsync_NullException_ThrowsArgumentNullException() {
        var botData = Substitute.For<ITelegramBotData>();
        var dispatcher = new Dispatcher<TestTelegramContext>(botData, []);
        var provider = new ServiceCollection().BuildServiceProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.HandleErrorAsync(null!, provider, TestContext.Current.CancellationToken));
    }

    // ---- DispatcherBuilder ------------------------------------------------------------------------------------

    public static IEnumerable<object[]> EmptyBuilders() {
        yield return [new DispatcherBuilder<TestTelegramContext>()];
        yield return [new DispatcherBuilder<TestTelegramContext, ValidController>()];
    }

    [Theory]
    [MemberData(nameof(EmptyBuilders))]
    public void DispatcherBuilder_BuildWithoutBotData_ThrowsTelegramBotAdvancedException(IDispatcherBuilder builder) {
        var exception = Assert.Throws<TelegramBotAdvancedException>(() => builder.Build());

        Assert.Equal(
            "Cannot build a Dispatcher without ITelegramBotData. Call SetTelegramBotData() before calling Build().",
            exception.Message);
    }

    [Fact]
    public void DispatcherBuilder_WithController_PrependsTypeParameterBeforeAddedControllers() {
        var botData = Substitute.For<ITelegramBotData>();
        var dispatcher = new DispatcherBuilder<TestTelegramContext, ValidController>()
            .SetTelegramBotData(botData)
            .AddControllers(typeof(SecondController))
            .Build();

        Assert.Equal([typeof(ValidController), typeof(SecondController)], dispatcher.GetControllersType());
    }

    [Fact]
    public void RegisterNewsletterController_AddsSubscriptionAndPublishingControllers() {
        var botData = Substitute.For<ITelegramBotData>();
        var dispatcher = new DispatcherBuilder<TestTelegramContext>()
            .RegisterNewsletterController<TestTelegramContext>()
            .SetTelegramBotData(botData)
            .Build();

        Assert.Contains(typeof(NewsletterSubscriptionController<TestTelegramContext>), dispatcher.GetControllersType());
        Assert.Contains(typeof(NewsletterPublishingController<TestTelegramContext>), dispatcher.GetControllersType());
    }
}
