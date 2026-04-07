using InventoryAlert.Api.Web.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Extensions.AssemblyFixture;

[assembly: TestFramework(AssemblyFixtureFramework.TypeName, AssemblyFixtureFramework.AssemblyName)]

namespace InventoryAlert.UnitTests.Fixtures;

public class InjectionFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }

    public InjectionFixture()
    {
        var services = new ServiceCollection();

        // Configuration setup - looking for appsettings.test.json or fall back to default
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Host=localhost;Database=test",
                ["Jwt:Key"] = "SuperSecretTestKeyThatIsLongEnough123456789",
                ["Auth:Username"] = "admin",
                ["Auth:Password"] = "password"
            })
            .Build();

        var settings = configuration.Get<AppSettings>() ?? new AppSettings();

        // Register configuration and app settings
        services.AddSingleton(settings);
        services.AddSingleton<IConfiguration>(configuration);

        // Register other services as needed for unit/integration tests
        // builder.Services.AddApplicationServices(); 

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
