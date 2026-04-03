
namespace InventoryAlert.Api
{
    public class AppSettings
    {
        public DatabaseSetting Database { get; set; }
        public Finnhub Finnhub { get; set; }
        public int MinuteSyncCurrentPrice { get; set; }
    }

    public class DatabaseSetting
    {
        public string DefaultConnection { get; set; }
    }

    public class Finnhub
    {
        public string ApiBaseUrl { get; set; }
        public string ApiKey { get; set; }
    }
}
