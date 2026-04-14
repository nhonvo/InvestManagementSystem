using FluentAssertions;
using InventoryAlert.Api.Configuration;
using InventoryAlert.Domain.Configuration;
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
        var settings = _fixture.ServiceProvider.GetRequiredService<ApiSettings>();

        // Assert
        settings.Should().NotBeNull();
        settings.Jwt.Issuer.Should().Be("TestIssuer");
        settings.Jwt.Audience.Should().Be("TestAudience");
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


