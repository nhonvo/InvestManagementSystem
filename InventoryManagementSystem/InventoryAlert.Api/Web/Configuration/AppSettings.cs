namespace InventoryAlert.Api.Web.Configuration
{
    public class AppSettings
    {
        public DatabaseSetting Database { get; set; } = new();
        public FinnhubSetting Finnhub { get; set; } = new();
        public int MinuteSyncCurrentPrice { get; set; }
    }

    public class DatabaseSetting
    {
        public string DefaultConnection { get; set; } = string.Empty;
    }

    public class FinnhubSetting
    {
        public string ApiBaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
