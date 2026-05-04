using FluentAssertions;
using InventoryAlert.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventoryAlert.IntegrationTests.Abstractions;

public abstract class HandlerTestBase : IClassFixture<TestFixture>, IAsyncLifetime
{
    protected readonly TestFixture Fixture;
    protected readonly IServiceProvider Provider;

    protected HandlerTestBase(TestFixture fixture)
    {
        Fixture = fixture;
        Provider = fixture.ServiceProvider;
    }

    public virtual async Task InitializeAsync()
    {
        await Fixture.ResetStateAsync();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected T GetService<T>() where T : notnull => Provider.GetRequiredService<T>();

    protected void AssertLog(string fragment)
    {
        Fixture.LoggerProvider.Entries.Should().Contain(e => e.Message.Contains(fragment));
    }
}
