using System.Net;
using FluentAssertions;
using InventoryAlert.Api.Infrastructure.External;
using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Contracts.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RestSharp;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.External;

public class FinnhubClientTests
{
    private readonly Mock<ILogger<FinnhubClient>> _logger = new();
    private readonly AppSettings _settings = new() { Finnhub = new SharedFinnhubSettings { ApiKey = "test-key" } };
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task GetQuote_ReturnsData_WhenSuccessful()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"c\":150.0}")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://finnhub.io/api/v1/") };
        var restClient = new RestClient(httpClient);

        var sut = new FinnhubClient(restClient, _settings, _logger.Object);

        var result = await sut.GetQuoteAsync("AAPL", Ct);

        result.Should().NotBeNull();
        result!.CurrentPrice.Should().Be(150m);
    }


    [Fact]
    public async Task GetProfile_ReturnsData_WhenSuccessful()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"name\":\"Apple Inc\",\"logo\":\"logo-url\"}")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://finnhub.io/api/v1/") };
        var restClient = new RestClient(httpClient);

        var sut = new FinnhubClient(restClient, _settings, _logger.Object);

        var result = await sut.GetProfileAsync("AAPL", Ct);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Apple Inc");
    }

    [Fact]
    public async Task SearchSymbols_ReturnsData_WhenSuccessful()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"count\":1,\"result\":[{\"symbol\":\"AAPL\",\"description\":\"APPLE INC\"}]}")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://finnhub.io/api/v1/") };
        var restClient = new RestClient(httpClient);

        var sut = new FinnhubClient(restClient, _settings, _logger.Object);

        var result = await sut.SearchSymbolsAsync("AAPL", Ct);

        result.Should().NotBeNull();
        result!.Result.Should().HaveCount(1);
        result.Result[0].Symbol.Should().Be("AAPL");
    }

    [Fact]
    public async Task GetPeers_ReturnsList_WhenSuccessful()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[\"AAPL\",\"MSFT\"]")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://finnhub.io/api/v1/") };
        var restClient = new RestClient(httpClient);

        var sut = new FinnhubClient(restClient, _settings, _logger.Object);

        var result = await sut.GetPeersAsync("AAPL", Ct);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain("MSFT");
    }

    [Fact]
    public async Task GetQuote_RetriesAndSucceeds_WhenInitialFailure()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network failure"))
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"c\":150.0}")
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://finnhub.io/api/v1/") };
        var restClient = new RestClient(httpClient);

        var sut = new FinnhubClient(restClient, _settings, _logger.Object);

        var result = await sut.GetQuoteAsync("AAPL", Ct);

        result.Should().NotBeNull();
        result!.CurrentPrice.Should().Be(150m);
        // Verify retry occurred (SendAsync called twice)
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}

public static class LoggerExtensions
{
    public static void VerifyLog<T>(this Mock<ILogger<T>> loggerMock, LogLevel level, Times times)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            times);
    }
}
