using FluentAssertions;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.UnitTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class ConfigurationTests(InjectionFixture fixture) : IClassFixture<InjectionFixture>
{
    private readonly InjectionFixture _fixture = fixture;

    [Fact]
    public void AppSettings_ShouldBeCorrectlyPopulated_FromFixture()
    {
        // Act
        var settings = _fixture.ServiceProvider.GetRequiredService<AppSettings>();

        // Assert
        settings.Should().NotBeNull();
        settings.Auth.Username.Should().Be("admin");
        settings.Jwt.Issuer.Should().Be("InventoryAlert.Api");
    }

    [Fact]
    public void ServiceProvider_ShouldResolve_AppSettings_AsSingleton()
    {
        // Act
        var firstInstance = _fixture.ServiceProvider.GetRequiredService<AppSettings>();
        var secondInstance = _fixture.ServiceProvider.GetRequiredService<AppSettings>();

        // Assert
        firstInstance.Should().BeSameAs(secondInstance);
    }
}
