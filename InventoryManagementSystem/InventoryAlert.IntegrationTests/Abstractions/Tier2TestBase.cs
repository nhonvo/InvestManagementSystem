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

    protected Tier2TestBase(TestFixture fixture, ITestOutputHelper output)
    {
        Fixture = fixture;
        Output = output;
        
        var baseUrl = Fixture.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8080/api/v1";
        
        var options = new RestClientOptions(baseUrl);
        Client = new RestClient(options, configureSerialization: s => s.UseSystemTextJson(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        }));
    }

    public virtual async Task InitializeAsync()
    {
        await Fixture.ResetStateAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected async Task<TestResult<T>> RunAction<T>(Func<Task<RestResponse<T>>> action)
    {
        return await Fixture.ApiActionConfig.RunActionAndViewLog(action);
    }
}
