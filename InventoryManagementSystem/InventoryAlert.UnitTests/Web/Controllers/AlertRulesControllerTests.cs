using System.Security.Claims;
using FluentAssertions;
using InventoryAlert.Api.Controllers;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Web.Controllers;

public class AlertRulesControllerTests
{
    private readonly Mock<IAlertRuleService> _service = new();
    private readonly AlertRulesController _sut;
    private static readonly CancellationToken Ct = CancellationToken.None;

    public AlertRulesControllerTests()
    {
        _sut = new AlertRulesController(_service.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new(ClaimTypes.NameIdentifier, "user-1"),
        }, "mock"));

        _sut.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task GetAlerts_Returns200_WithItems()
    {
        var items = new List<AlertRuleResponse> {
            new(Guid.NewGuid(), "AAPL", AlertCondition.PriceBelow, 150m, true, true, null)
        };
        _service.Setup(s => s.GetByUserIdAsync("user-1", Ct)).ReturnsAsync(items);

        var result = await _sut.GetAlerts(Ct);

        var ok = (result.Result as OkObjectResult);
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task Create_Returns201_WhenSuccessful()
    {
        var request = new AlertRuleRequest("AAPL", AlertCondition.PriceBelow, 150m, true);
        var response = new AlertRuleResponse(Guid.NewGuid(), "AAPL", AlertCondition.PriceBelow, 150m, true, true, null);
        _service.Setup(s => s.CreateAsync(request, "user-1", Ct)).ReturnsAsync(response);

        var result = await _sut.Create(request, Ct);

        var created = (result.Result as CreatedAtActionResult);
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task Update_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new AlertRuleRequest("NEW", AlertCondition.PriceBelow, 150m, true);
        var response = new AlertRuleResponse(id, "NEW", AlertCondition.PriceBelow, 150m, true, true, null);
        _service.Setup(s => s.UpdateAsync(id, request, "user-1", Ct)).ReturnsAsync(response);

        // Act
        var result = await _sut.Update(id, request, Ct);

        // Assert
        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(response);
    }
}

