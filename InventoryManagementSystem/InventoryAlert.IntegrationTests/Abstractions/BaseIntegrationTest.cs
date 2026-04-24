using InventoryAlert.IntegrationTests.Config;
using InventoryAlert.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace InventoryAlert.IntegrationTests.Abstractions;

public class BaseIntegrationTest : IClassFixture<InjectionFixture>
{
    protected readonly InjectionFixture _fixture;
    protected readonly TestUser _testUser;
    protected readonly ITestOutputHelper _output;

    public BaseIntegrationTest(InjectionFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        var appSettings = fixture.ServiceProvider.GetRequiredService<AppSettings>();
        _testUser = appSettings.TestUser;
    }
}
