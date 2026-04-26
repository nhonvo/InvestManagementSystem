using FluentAssertions;
using InventoryAlert.Api.Middleware;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Middleware;

public class CorrelationIdMiddlewareTests
{
    private readonly Mock<ICorrelationProvider> _correlationProvider = new();
    private readonly CorrelationIdMiddleware _sut;

    public CorrelationIdMiddlewareTests()
    {
        _sut = new CorrelationIdMiddleware(_correlationProvider.Object);
    }

    [Fact]
    public async Task InvokeAsync_GeneratesNewId_WhenHeaderMissing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        context.Response.Headers.ContainsKey("X-Correlation-Id").Should().BeTrue();
        var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
        context.Items["X-Correlation-Id"].Should().Be(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_UsesExistingId_WhenHeaderPresent()
    {
        // Arrange
        var existingId = "existing-correlation-id";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = existingId;
        RequestDelegate next = (ctx) => Task.CompletedTask;

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        context.Response.Headers["X-Correlation-Id"].ToString().Should().Be(existingId);
        context.Items["X-Correlation-Id"].Should().Be(existingId);
    }
}

