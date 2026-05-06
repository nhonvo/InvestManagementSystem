using InventoryAlert.IntegrationTests.Models.Request;
using InventoryAlert.IntegrationTests.Models.Response;
using RestSharp;

namespace InventoryAlert.IntegrationTests.Clients;

public class WiremockAdminClient
{
    private readonly RestClient _client;

    public WiremockAdminClient(RestClient client)
    {
        _client = client;
    }

    public async Task<RestResponse> ResetAsync()
    {
        var request = new RestRequest("/reset");
        return await _client.ExecutePostAsync(request);
    }

    public async Task<RestResponse> ResetWithoutRemoveStubsMappingAsync()
    {
        var request = new RestRequest("/mapping/reset");
        return await _client.ExecutePostAsync(request);
    }

    public async Task<RestResponse<WiremockCountResponse>> GetCountOfGetQuotesRequestsAsync()
    {
        var body = new WiremockCountGetQuoteRequest()
        {
            Method = "GET",
            UrlPath = "/quote",
            Headers = new Dictionary<string, MatchPattern>
            {
                ["X-Finnhub-Token"] = new MatchPattern
                {
                    Matches = ".+"
                }
            },
            QueryParameters = new Dictionary<string, MatchPattern>
            {
                ["symbol"] = new MatchPattern
                {
                    Matches = ".+"
                }
            }
        };

        var request = new RestRequest("/requests/count");
        request.AddJsonBody(body);
        return await _client.ExecutePostAsync<WiremockCountResponse>(request);
    }
}
