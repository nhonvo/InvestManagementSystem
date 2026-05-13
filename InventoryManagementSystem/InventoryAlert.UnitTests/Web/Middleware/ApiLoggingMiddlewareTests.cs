using System.Text;
using InventoryAlert.Api.Middleware;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.UnitTests.Infrastructure.External;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace InventoryAlert.UnitTests.Web.Middleware;

public class ApiLoggingMiddlewareTests
{
    private readonly Mock<ILogger<ApiLoggingMiddleware>> _logger = new();
    private readonly AppSettings _settings = new();
    private readonly ApiLoggingMiddleware _sut;

    public ApiLoggingMiddlewareTests()
    {
        _sut = new ApiLoggingMiddleware(_logger.Object, _settings);
    }

    [Fact]
    public async Task InvokeAsync_LogsInformation_WhenFast()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
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
        context.Request.Path = "/api/test";
        RequestDelegate next = async (ctx) => await Task.Delay(600); // Simulate slow request

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        _logger.VerifyLog(LogLevel.Warning, Times.Once());
    }

    [Fact]
    public async Task InvokeAsync_LogsError_WhenStatusCodeIs500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        RequestDelegate next = (ctx) =>
        {
            ctx.Response.StatusCode = 500;
            return Task.CompletedTask;
        };

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        _logger.VerifyLog(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task InvokeAsync_LogsRequestBody_WhenApiRequestHasBody()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Method = "POST";
        var body = "{\"name\":\"test-item\"}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        
        RequestDelegate next = (ctx) => Task.CompletedTask;

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        VerifyLogProperty("RequestBody", "test-item");
    }

    [Fact]
    public async Task InvokeAsync_LogsResponseBody_WhenApiResponseHasBody()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        
        RequestDelegate next = async (ctx) =>
        {
            var responseBody = "{\"status\":\"success\"}";
            await ctx.Response.WriteAsync(responseBody);
        };

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        VerifyLogProperty("ResponseBody", "success");
    }

    [Fact]
    public async Task InvokeAsync_RedactsPassword_InRequestBody()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        var body = "{\"username\":\"admin\",\"password\":\"secret123\"}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        
        RequestDelegate next = (ctx) => Task.CompletedTask;

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        VerifyLogProperty("RequestBody", "[REDACTED]");
    }

    [Fact]
    public async Task InvokeAsync_TruncatesLongBody()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        var longBody = new string('a', 5000);
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(longBody));
        
        RequestDelegate next = (ctx) => Task.CompletedTask;

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        VerifyLogProperty("RequestBody", "[TRUNCATED]");
    }

    [Fact]
    public async Task InvokeAsync_SkipsBodyLogging_WhenDisabledInSettings()
    {
        // Arrange
        _settings.Api = new AppSettings.ApiOptions { EnableBodyLogging = false };
        var sut = new ApiLoggingMiddleware(_logger.Object, _settings);
        
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        var body = "{\"name\":\"test\"}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        
        RequestDelegate next = (ctx) => Task.CompletedTask;

        // Act
        await sut.InvokeAsync(context, next);

        // Assert
        // Minimal log shouldn't have RequestBody property
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() != null && !v.ToString()!.Contains("Req:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyLogProperty(string key, string expectedValuePartial)
    {
        _logger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() != null && v.ToString()!.Contains(expectedValuePartial)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
