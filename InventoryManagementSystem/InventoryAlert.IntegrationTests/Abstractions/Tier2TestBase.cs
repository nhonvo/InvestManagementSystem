using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using RestSharp;
using RestSharp.Serializers.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace InventoryAlert.IntegrationTests.Abstractions;

[Collection("IntegrationTests")]
public abstract class Tier2TestBase : IAsyncLifetime
{
    protected readonly TestFixture Fixture;
    protected readonly ITestOutputHelper Output;
    protected readonly RestClient Client;
    protected readonly IServiceProvider Services;
    private readonly IServiceScope _scope;

    protected Tier2TestBase(TestFixture fixture, ITestOutputHelper output)
    {
        Fixture = fixture;
        Output = output;
        
        // Use the in-process HttpClient from the factory
        var httpClient = Fixture.CreateTestClient();
        
        // Wrap it in RestClient for the tests that use RestSharp
        var options = new RestClientOptions
        {
            BaseUrl = new Uri(httpClient.BaseAddress!, "api/v1/")
        };
        
        Client = new RestClient(httpClient, options, configureSerialization: s => s.UseSystemTextJson(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        }));

        _scope = fixture.Services.CreateScope();
        Services = _scope.ServiceProvider;
    }

    public virtual async Task InitializeAsync()
    {
        await Fixture.ResetStateAsync();
    }

    public virtual Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<TestResult<T>> RunAction<T>(Func<Task<RestResponse<T>>> action)
    {
        return await Fixture.ApiActionConfig.RunActionAndViewLog(action);
    }
}
