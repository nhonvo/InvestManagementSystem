using RestSharp;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.IntegrationTests.Abstractions;

namespace InventoryAlert.IntegrationTests.Clients;

public class EventClient(RestClient client) : BaseClient(client)
{
    public async Task<RestResponse> PublishEventAsync(string accessToken, string eventType, object payload)
    {
        var request = new RestRequest("/events");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddJsonBody(new PublishEventRequest
        {
            EventType = eventType,
            Payload = payload
        });

        return await _client.ExecutePostAsync(request);
    }
}
