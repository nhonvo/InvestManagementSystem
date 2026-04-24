namespace InventoryAlert.IntegrationTests.Config;

public class WiremockSettings
{
    public string BaseUrl { get; set; } = "http://wiremock:9091";
    public string AdminUrl { get; set; } = "http://wiremock:9091/__admin";
    public int TimeoutSeconds { get; set; } = 30;
}
