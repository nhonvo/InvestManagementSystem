namespace InventoryAlert.IntegrationTests.Config;

public class AppSettings
{
    public ApiSettings ApiSettings { get; set; } = new ApiSettings();
    public WiremockSettings WiremockSettings { get; set; } = new WiremockSettings();
    public TestUser TestUser { get; set; } = new TestUser();
}
