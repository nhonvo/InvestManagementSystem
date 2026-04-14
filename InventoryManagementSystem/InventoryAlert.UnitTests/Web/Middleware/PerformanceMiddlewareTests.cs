using InventoryAlert.Api.Middleware;
using InventoryAlert.UnitTests.Infrastructure.External;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Middleware;

public class PerformanceMiddlewareTests
{
    private readonly Mock<ILoggerFactory> _loggerFactory = new();
    private readonly Mock<ILogger<PerformanceMiddleware>> _logger = new();
    private readonly PerformanceMiddleware _sut;

    public PerformanceMiddlewareTests()
    {
        _loggerFactory.Setup(x => x.CreateLogger(typeof(PerformanceMiddleware).FullName!))
            .Returns(_logger.Object);
        _sut = new PerformanceMiddleware(_loggerFactory.Object);
    }

    [Fact]
    public async Task InvokeAsync_LogsInformation_WhenFast()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        _logger.VerifyLog(LogLevel.Information, Times.Once());
    }

    [Fact]
    public async Task InvokeAsync_LogsWarning_WhenSlow()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = async (ctx) => await Task.Delay(600); // Simulate slow request

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        _logger.VerifyLog(LogLevel.Warning, Times.Once());
    }
}

