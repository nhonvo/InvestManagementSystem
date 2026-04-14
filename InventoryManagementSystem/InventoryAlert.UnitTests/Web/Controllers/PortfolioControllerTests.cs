using System.Security.Claims;
using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class PortfolioControllerTests
{
    private readonly Mock<IPortfolioService> _service = new();
    private readonly PortfolioController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private const string UserId = "user-1";

    public PortfolioControllerTests()
    {
        _sut = new PortfolioController(_service.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new(ClaimTypes.NameIdentifier, UserId),
        }, "mock"));

        _sut.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task GetPortfolio_Returns200_WithPagedResult()
    {
        // Arrange
        var query = new PortfolioQueryParams { PageNumber = 1, PageSize = 10 };
        var pagedResult = new PagedResult<PortfolioPositionResponse>
        {
            Items = new List<PortfolioPositionResponse>(),
            TotalItems = 0,
            PageNumber = 1,
            PageSize = 10
        };
        _service.Setup(s => s.GetPositionsPagedAsync(query, UserId, Ct)).ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetPositions(query, Ct);

        // Assert
        var ok = (result.Result as OkObjectResult);
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task OpenPosition_Returns201_WhenSuccessful()
    {
        // Arrange
        var request = new CreatePositionRequest("AAPL", 10, 150m, null);
        var response = new PortfolioPositionResponse(1, "AAPL", "Apple", "NASDAQ", "logo", 10, 150m, 160m, 1600m, 1500m, 100m, 6.67, 10m, 6.67m, "Tech");
        _service.Setup(s => s.OpenPositionAsync(request, UserId, Ct)).ReturnsAsync(response);

        // Act
        var result = await _sut.OpenPosition(request, Ct);

        // Assert
        var created = (result.Result as CreatedAtActionResult);
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task RemovePosition_Returns204_OnSuccess()
    {
        // Act
        var result = await _sut.RemovePosition("TSLA", Ct);

        // Assert
        var noContent = (result as NoContentResult);
        noContent.Should().NotBeNull();
        noContent!.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        _service.Verify(s => s.RemovePositionAsync("TSLA", UserId, Ct), Times.Once);
    }
}
