using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;
using RestSharp;
using RestSharp.Serializers.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryAlert.IntegrationTests.Config;
using Xunit;

namespace InventoryAlert.IntegrationTests.Abstractions;

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly TestFixture Fixture;
    protected readonly ITestOutputHelper Output;
    protected readonly RestClient Client;
    protected readonly TestUser _testUser;

    protected BaseIntegrationTest(TestFixture fixture, ITestOutputHelper output)
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
        
        var appSettings = Fixture.Configuration.Get<AppSettings>();
        _testUser = appSettings?.TestUser ?? new TestUser { Username = "admin", Password = "password" };
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
