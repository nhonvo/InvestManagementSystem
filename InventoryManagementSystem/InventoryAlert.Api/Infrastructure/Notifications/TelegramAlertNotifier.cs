using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Domain.Constants;
using InventoryAlert.Api.Web.Configuration;

namespace InventoryAlert.Api.Infrastructure.Notifications;

/// <summary>
/// Sends alert messages to a Telegram bot chat via HTTP Bot API.
/// Configure via appsettings: Telegram:BotToken and Telegram:ChatId.
/// Replaces ConsoleAlertNotifier when Phase F is activated.
/// </summary>
public sealed class TelegramAlertNotifier(
    IHttpClientFactory httpClientFactory,
    AppSettings settings,
    ILogger<TelegramAlertNotifier> logger) : IAlertNotifier
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly string _botToken = settings.Telegram.BotToken;
    private readonly string _chatId = settings.Telegram.ChatId;
    private readonly ILogger<TelegramAlertNotifier> _logger = logger;

    public async Task NotifyAsync(string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(_chatId))
        {
            _logger.LogWarning("[TelegramAlertNotifier] BotToken or ChatId not configured. Skipping notification.");
            return;
        }

        try
        {
            var http = _httpClientFactory.CreateClient(ApplicationConstants.HttpClientNames.Telegram);
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";

            var payload = new { chat_id = _chatId, text = message, parse_mode = "Markdown" };
            var response = await http.PostAsJsonAsync(url, payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("[TelegramAlertNotifier] Telegram API error {StatusCode}: {Body}",
                    (int)response.StatusCode, body);
            }
            else
            {
                _logger.LogInformation("[TelegramAlertNotifier] Alert sent to chat {ChatId}.", _chatId);
            }
        }
        catch (Exception ex)
        {
            // Never crash the app if Telegram is unreachable
            _logger.LogError(ex, "[TelegramAlertNotifier] Failed to send Telegram notification.");
        }
    }
}
