namespace Telegram.Bot.Advanced.Extensions {
    public class TelegramRoutingOptions {
        /// <summary>
        /// Set the base URL to be used to update the Telegram's webhook
        /// </summary>
        public string? WebhookBaseUrl { get; set; } = null;
        
        /// <summary>
        /// Fully qualified webhook URL. This will override the default server configuration {WebhookBaseurl}/{BasePath}/{Endpoint}
        /// </summary>
        public string? WebhookUrl { get; set; } = null;
        
        /// <summary>
        /// If the server should update the Telegram's webhook configuration on startup. Default is true
        /// </summary>
        public bool UpdateWebhook { get; set; } = true;
    }
}