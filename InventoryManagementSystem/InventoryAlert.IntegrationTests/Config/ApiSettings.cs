namespace InventoryAlert.IntegrationTests.Config;

public class ApiSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public int TimeoutSeconds { get; set; } = 30;
}
