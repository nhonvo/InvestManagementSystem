using FluentAssertions;
using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventoryAlert.IntegrationTests.Abstractions;

[Collection("IntegrationTests")]
public abstract class HandlerTestBase : IAsyncLifetime
{
    protected readonly TestFixture Fixture;
    protected readonly IServiceProvider Provider;
    private readonly IServiceScope _scope;

    protected HandlerTestBase(TestFixture fixture)
    {
        Fixture = fixture;
        _scope = fixture.Services.CreateScope();
        Provider = _scope.ServiceProvider;
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

    protected T GetService<T>() where T : notnull => Provider.GetRequiredService<T>();

    protected void AssertLog(string fragment)
    {
        Fixture.LoggerProvider.Entries.Should().Contain(e => e.Message.Contains(fragment));
    }
}
