namespace Telegram.Bot.Advanced.IntegrationTests.Infrastructure;

/// <summary>
/// Gates the live integration lanes on environment variables. Every getter returns <c>null</c> (or a documented
/// default) when its variable is missing or whitespace-only, so lanes without credentials skip instead of failing.
/// </summary>
public static class IntegrationSettings {
    private static readonly string GeneratedSecret = "tba_it_" + Guid.NewGuid().ToString("N");

    public static string? BotToken => Read("TBA_IT_BOT_TOKEN");

    public static long? ChatId {
        get {
            var raw = Read("TBA_IT_CHAT_ID");
            return raw is not null && long.TryParse(raw, out var value) ? value : null;
        }
    }

    public static Uri? WebhookBaseUrl {
        get {
            var raw = Read("TBA_IT_WEBHOOK_BASE_URL");
            return raw is not null && Uri.TryCreate(raw, UriKind.Absolute, out var uri) ? uri : null;
        }
    }

    public static int? WebhookLocalPort {
        get {
            var raw = Read("TBA_IT_WEBHOOK_LOCAL_PORT");
            return raw is not null && int.TryParse(raw, out var value) ? value : null;
        }
    }

    public static string WebhookSecret => Read("TBA_IT_WEBHOOK_SECRET") ?? GeneratedSecret;

    public static int InboundWaitSeconds {
        get {
            var raw = Read("TBA_IT_INBOUND_WAIT_SECONDS");
            return raw is not null && int.TryParse(raw, out var value) ? value : 0;
        }
    }

    public static string RequireToken() {
        Assert.SkipUnless(BotToken is not null, "Set TBA_IT_BOT_TOKEN to a real bot token to run live Telegram integration tests.");
        return BotToken!;
    }

    public static long RequireChatId() {
        Assert.SkipUnless(ChatId is not null,
            "Set TBA_IT_CHAT_ID to a chat the bot may post to (start the bot in that private chat, or add it to that group).");
        return ChatId!.Value;
    }

    public static (Uri BaseUrl, int LocalPort) RequireWebhook() {
        Assert.SkipUnless(WebhookBaseUrl is not null && WebhookLocalPort is not null,
            "Set TBA_IT_WEBHOOK_BASE_URL (public HTTPS base, e.g. an ngrok URL) and TBA_IT_WEBHOOK_LOCAL_PORT (the local port it forwards to) to run live webhook tests.");
        return (WebhookBaseUrl!, WebhookLocalPort!.Value);
    }

    private static string? Read(string name) {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
