namespace Telegram.Bot.Advanced.IntegrationTests.Infrastructure;

/// <summary>
/// Configuration for <see cref="TestWebhookHost.StartAsync"/>. <see cref="Port"/> must be a reserved, non-zero
/// port when <see cref="RegisterWebhookTransport"/> is true: the webhook URL registered with Telegram (or the fake
/// API) must be known before the host binds, so an ephemeral (zero) port cannot be used together with the webhook
/// transport.
/// </summary>
public sealed class TestWebhookHostOptions {
    public string Endpoint { get; init; } = "itbot";
    public string BasePath { get; init; } = "/telegram/";
    public required string SecretToken { get; init; }
    public Uri? PublicBaseUri { get; init; }
    public int Port { get; init; }
    public string? RealBotToken { get; init; }
    public bool RegisterWebhookTransport { get; init; } = true;
    public bool DeleteWebhookOnShutdown { get; init; } = true;
}
