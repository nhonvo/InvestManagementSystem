using InventoryAlert.IntegrationTests.Config;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace InventoryAlert.IntegrationTests.Clients;

public class BaseClient
{
    protected readonly RestClient _client;

    // public BaseClient(AppSettings settings)
    // {
    //     var options = new RestClientOptions(settings.ApiSettings.BaseUrl)
    //     {
    //         Timeout = TimeSpan.FromSeconds(settings.ApiSettings.TimeoutSeconds)
    //     };
    //     _client = new RestClient(
    //         options,
    //         configureSerialization: s => s.UseNewtonsoftJson()
    //     );
    // }

    public BaseClient(RestClient client)
    {
        _client = client;
    }
}
