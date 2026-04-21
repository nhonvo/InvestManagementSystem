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
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
            .Build();

        var settings = configuration.Get<AppSettings>() ?? new AppSettings();

        services.AddSingleton(settings);
        services.AddSingleton<IConfiguration>(configuration);

        var options = new RestClientOptions(settings.ApiSettings.BaseUrl)
        {
            Timeout = TimeSpan.FromSeconds(settings.ApiSettings.TimeoutSeconds)
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
