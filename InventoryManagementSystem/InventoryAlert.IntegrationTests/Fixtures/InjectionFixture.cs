using System.Reflection;
using InventoryAlert.IntegrationTests.Clients;
using InventoryAlert.IntegrationTests.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace InventoryAlert.IntegrationTests.Fixtures;

public class InjectionFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }

    public InjectionFixture()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ApiSettings:BaseUrl", "http://localhost:8080/api/v1" },
                { "ApiSettings:TimeoutSeconds", "30" },
                { "WiremockSettings:BaseUrl", "http://localhost:9091" },
                { "WiremockSettings:AdminUrl", "http://localhost:9091/__admin" },
                { "TestUser:Username", "admin" },
                { "TestUser:Password", "password" }
            })
            .Build();

        var settings = configuration.Get<AppSettings>() ?? throw new InvalidOperationException("AppSettings could not be loaded from embedded resource.");

        services.AddSingleton(settings);
        services.AddSingleton<IConfiguration>(configuration);

        var options = new RestClientOptions(settings.ApiSettings.BaseUrl)
        {
            Timeout = TimeSpan.FromSeconds(settings.ApiSettings.TimeoutSeconds),
            CookieContainer = new System.Net.CookieContainer()
        };
        services.AddSingleton(new RestClient(options, configureSerialization: s => s.UseNewtonsoftJson()));

        var wiremockOptions = new RestClientOptions(settings.WiremockSettings.AdminUrl)
        {
            Timeout = TimeSpan.FromSeconds(settings.WiremockSettings.TimeoutSeconds)
        };
        var wiremockRestClient = new RestClient(wiremockOptions, configureSerialization: s => s.UseNewtonsoftJson());
        services.AddSingleton(new WiremockAdminClient(wiremockRestClient));

        var mockFinnhubOptions = new RestClientOptions(settings.WiremockSettings.BaseUrl)
        {
            Timeout = TimeSpan.FromSeconds(settings.WiremockSettings.TimeoutSeconds)
        };
        var mockFinnhubRestClient = new RestClient(mockFinnhubOptions, configureSerialization: s => s.UseNewtonsoftJson());
        services.AddSingleton(new MockFinnhubClient(mockFinnhubRestClient));

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
