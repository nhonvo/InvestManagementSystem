using InventoryAlert.Contracts.Configuration;

namespace InventoryAlert.Api.Web.Configuration;

public class AppSettings
{
    public AuthSettings Auth { get; set; } = new();
    public SharedAwsSettings Aws { get; set; } = new();
    public SharedDatabaseSettings Database { get; set; } = new();
    public SharedFinnhubSettings Finnhub { get; set; } = new();
    public RedisSettings Redis { get; set; } = new();
    public CacheSettings Cache { get; set; } = new();
    public JwtSettings Jwt { get; set; } = new();
    public SharedSeqSettings Seq { get; set; } = new();
    public TelegramSettings Telegram { get; set; } = new();
    public int MinuteSyncCurrentPrice { get; set; }
}
