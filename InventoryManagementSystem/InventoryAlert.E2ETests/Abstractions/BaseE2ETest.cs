using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using InventoryAlert.Domain.DTOs;
using RestSharp;
using RestSharp.Serializers.Json;

namespace InventoryAlert.E2ETests.Abstractions;

public abstract class BaseE2ETest : IDisposable
{
    protected readonly RestClient Client;
    protected readonly string BaseUrl = "http://localhost:8080";
    protected string? JwtToken;

    protected BaseE2ETest()
    {
        var options = new RestClientOptions(BaseUrl)
        {
            ThrowOnAnyError = false
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        Client = new RestClient(options, configureSerialization: s => s.UseSystemTextJson(jsonOptions));
    }

    protected async Task EnsureAuthenticatedAsync()
    {
        if (JwtToken != null) return;

        var request = new RestRequest("api/v1/auth/login", Method.Post);
        request.AddJsonBody(new LoginRequest("admin", "password"));

        var response = await Client.ExecuteAsync<AuthResponse>(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Login should succeed for default admin");
        response.Data.Should().NotBeNull();

        JwtToken = response.Data!.AccessToken;
    }

    protected RestRequest CreateAuthenticatedRequest(string resource, Method method)
    {
        if (string.IsNullOrEmpty(JwtToken))
        {
            throw new InvalidOperationException("Must call EnsureAuthenticatedAsync first.");
        }

        var request = new RestRequest(resource, method);
        request.AddHeader("Authorization", $"Bearer {JwtToken}");
        return request;
    }

    public void Dispose()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
    }
}
