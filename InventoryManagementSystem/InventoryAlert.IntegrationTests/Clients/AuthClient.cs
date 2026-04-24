using RestSharp;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.IntegrationTests.Abstractions;

namespace InventoryAlert.IntegrationTests.Clients;

public class AuthClient(RestClient client) : BaseClient(client)
{
    public async Task<RestResponse<AuthResponse>> LoginAsync(string username, string password)
    {
        var request = new RestRequest("/Auth/login");
        request.AddJsonBody(new LoginRequest(username, password));

        return await _client.ExecutePostAsync<AuthResponse>(request);
    }

    public async Task<RestResponse> RegisterAsync(string username, string password, string email)
    {
        var request = new RestRequest("/Auth/register");
        request.AddJsonBody(new RegisterRequest(username, password, email));

        return await _client.ExecutePostAsync<RegistrationResponse>(request);
    }

    public async Task<RestResponse<AuthResponse>> RefreshTokenAsync(string refreshToken)
    {
        var request = new RestRequest("/Auth/refresh");
        request.AddCookie("refreshToken", refreshToken);

        return await _client.ExecutePostAsync<AuthResponse>(request);
    }

    public async Task<RestResponse> LogoutAsync(string refreshToken)
    {
        var request = new RestRequest("/Auth/logout");
        request.AddHeader("Authorization", $"Bearer {refreshToken}");

        return await _client.ExecutePostAsync(request);
    }
}
