using RestSharp;

namespace InventoryAlert.IntegrationTests.Abstractions;

public class BaseClient
{
    protected readonly RestClient _client;

    public BaseClient(RestClient client)
    {
        _client = client;
    }
}
