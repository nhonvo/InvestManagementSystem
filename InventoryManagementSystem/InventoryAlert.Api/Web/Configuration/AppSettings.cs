using InventoryAlert.Contracts.Configuration;

namespace InventoryAlert.Api.Web.Configuration;

public class AppSettings
{
    public AuthSettings Auth { get; set; } = new();
    public SharedAwsSettings Aws { get; set; } = new();
    public CacheSettings Cache { get; set; } = new();
    public SharedDatabaseSettings Database { get; set; } = new();
    public SharedFinnhubSettings Finnhub { get; set; } = new();
    public RedisSettings Redis { get; set; } = new();
    public JwtSettings Jwt { get; set; } = new();
    public int MinuteSyncCurrentPrice { get; set; }
    public TelegramSetting Telegram { get; set; } = new();
}

public class CacheSettings
{
    public int ProductTtlMinutes { get; set; } = 10;
}

public class AuthSettings
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public class TelegramSetting
{
    /// <summary>Telegram Bot API token. Leave empty to disable notifications.</summary>
    public string BotToken { get; set; } = string.Empty;
    /// <summary>Target chat/channel ID to send alerts to.</summary>
    public string ChatId { get; set; } = string.Empty;
}

