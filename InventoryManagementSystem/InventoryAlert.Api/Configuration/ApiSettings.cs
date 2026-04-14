using InventoryAlert.Domain.Configuration;

namespace InventoryAlert.Api.Configuration;

public class ApiSettings : AppSettings
{
    public JwtSettings Jwt { get; set; } = new();

}
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
