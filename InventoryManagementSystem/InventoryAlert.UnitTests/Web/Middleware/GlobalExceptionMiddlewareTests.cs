using System.Net;
using FluentAssertions;
using InventoryAlert.Api.Application.Common.Exceptions;
using InventoryAlert.Api.Domain.Constants;
using InventoryAlert.Api.Web.Middleware;
using InventoryAlert.Api.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private readonly Mock<ILoggerFactory> _loggerFactory = new();
    private readonly Mock<ILogger<GlobalExceptionMiddleware>> _logger = new();
    private readonly GlobalExceptionMiddleware _sut;

    public GlobalExceptionMiddlewareTests()
    {
        _loggerFactory.Setup(x => x.CreateLogger(typeof(GlobalExceptionMiddleware).FullName!))
            .Returns(_logger.Object);
        _sut = new GlobalExceptionMiddleware(_loggerFactory.Object);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext_WhenNoException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(ErrorCode.NotFound, HttpStatusCode.NotFound)]
    [InlineData(ErrorCode.Conflict, HttpStatusCode.Conflict)]
    [InlineData(ErrorCode.BadRequest, HttpStatusCode.BadRequest)]
    [InlineData(ErrorCode.Unauthorized, HttpStatusCode.Unauthorized)]
    [InlineData(ErrorCode.Internal, HttpStatusCode.InternalServerError)]
    [InlineData(ErrorCode.UnprocessableEntity, HttpStatusCode.UnprocessableEntity)]
    public async Task InvokeAsync_HandlesUserFriendlyException_WithCorrectStatus(ErrorCode errorCode, HttpStatusCode expectedStatus)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream(); // Capture output
        var exception = new UserFriendlyException(errorCode, "Test error");
        RequestDelegate next = (_) => throw exception;

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.Should().Be((int)expectedStatus);
        context.Response.ContentType.Should().Be("application/json");
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("Test error");
    }

    [Fact]
    public async Task InvokeAsync_HandlesValidationException_ReturnsBadRequest()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new InventoryAlert.Api.Domain.Exceptions.ValidationException("Validation failed");
        RequestDelegate next = (_) => throw exception;

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("Validation failed");
    }

    [Fact]
    public async Task InvokeAsync_HandlesGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var exception = new Exception("Critical failure");
        RequestDelegate next = (_) => throw exception;

        // Act
        await _sut.InvokeAsync(context, next);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        responseBody.Should().Contain("An error has occurred.");
    }
}
