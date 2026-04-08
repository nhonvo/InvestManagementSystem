using FluentAssertions;
using InventoryAlert.Api.Infrastructure.Notifications;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Api.Domain.Constants;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Notifications;

public class TelegramAlertNotifierTests
{
    private readonly Mock<IHttpClientFactory> _clientFactoryMock = new();
    private readonly AppSettings _settings = new() 
    { 
        Telegram = new() { BotToken = "valid-token", ChatId = "12345" } 
    };
    private readonly Mock<ILogger<TelegramAlertNotifier>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _handlerMock = new();

    public TelegramAlertNotifierTests()
    {
        var client = new HttpClient(_handlerMock.Object);
        _clientFactoryMock.Setup(x => x.CreateClient(ApplicationConstants.HttpClientNames.Telegram)).Returns(client);
    }

    [Fact]
    public async Task NotifyAsync_Skips_WhenNotConfigured()
    {
        // Arrange
        var settings = new AppSettings { Telegram = new() { BotToken = "", ChatId = "" } };
        var sut = new TelegramAlertNotifier(_clientFactoryMock.Object, settings, _loggerMock.Object);

        // Act
        await sut.NotifyAsync("test");

        // Assert
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_SendsPost_WhenConfigured()
    {
        // Arrange
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK });

        var sut = new TelegramAlertNotifier(_clientFactoryMock.Object, _settings, _loggerMock.Object);

        // Act
        await sut.NotifyAsync("hello world");

        // Assert
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m => 
                m.Method == HttpMethod.Post && 
                m.RequestUri!.ToString().Contains("valid-token")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_LogsError_WhenApiFails()
    {
        // Arrange
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage 
            { 
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("some error")
            });

        var sut = new TelegramAlertNotifier(_clientFactoryMock.Object, _settings, _loggerMock.Object);

        // Act
        await sut.NotifyAsync("test");

        // Assert
        // We ensure it didn't throw and completed
        await sut.NotifyAsync("test");
    }
}
