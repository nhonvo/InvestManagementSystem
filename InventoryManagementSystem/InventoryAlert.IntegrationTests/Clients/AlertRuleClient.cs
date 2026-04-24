using InventoryAlert.Domain.DTOs;
using InventoryAlert.IntegrationTests.Abstractions;
using RestSharp;

namespace InventoryAlert.IntegrationTests.Clients;

public class AlertRuleClient : BaseClient
{
    public AlertRuleClient(RestClient client) : base(client) { }

    public async Task<RestResponse<List<AlertRuleResponse>>> GetAlertRulesAsync(string accessToken)
    {
        var request = new RestRequest("/AlertRules");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        return await _client.ExecuteGetAsync<List<AlertRuleResponse>>(request);
    }

    public async Task<RestResponse<AlertRuleResponse>> CreateAlertRuleAsync(string accessToken, AlertRuleRequest alertRuleRequest)
    {
        var request = new RestRequest("/AlertRules");
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        request.AddJsonBody(alertRuleRequest);
        return await _client.ExecutePostAsync<AlertRuleResponse>(request);
    }

    public async Task<RestResponse> DeleteAlertRuleAsync(string accessToken, Guid id)
    {
        var request = new RestRequest("/AlertRules/{id}");
        request.AddUrlSegment("id", id);
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        return await _client.ExecuteDeleteAsync(request);
    }

    public async Task<RestResponse<AlertRuleResponse>> ToggleAlertRuleAsync(string accessToken, Guid id, bool isActive)
    {
        var request = new RestRequest("/AlertRules/{id}/toggle");
        request.AddUrlSegment("id", id);
        request.AddHeader("Authorization", $"Bearer {accessToken}");
        var body = new ToggleAlertRequest(isActive);
        //{
        //    IsActive = isActive
        //};
        request.AddJsonBody(body);
        return await _client.ExecutePatchAsync<AlertRuleResponse>(request);
    }
}
