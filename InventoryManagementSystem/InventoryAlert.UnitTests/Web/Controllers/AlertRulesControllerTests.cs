using FluentAssertions;
using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
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
            new(Guid.NewGuid(), "user-1", "AAPL", "Price", "Below", 150m, "telegram", true, null, DateTime.UtcNow) 
        };
        _service.Setup(s => s.GetUserAlertsAsync("user-1", Ct)).ReturnsAsync(items);

        var result = await _sut.GetAlerts(Ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
        ok.Value.Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task CreateAlert_Returns201_WhenSuccessful()
    {
        var request = new AlertRuleRequest("AAPL", "Price", "Below", 150m, "telegram");
        var response = new AlertRuleResponse(Guid.NewGuid(), "user-1", "AAPL", "Price", "Below", 150m, "telegram", true, null, DateTime.UtcNow);
        _service.Setup(s => s.CreateAlertAsync("user-1", request, Ct)).ReturnsAsync(response);

        var result = await _sut.CreateAlert(request, Ct);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task DeleteAlert_Returns204_WhenSuccessful()
    {
        var ruleId = Guid.NewGuid();
        _service.Setup(s => s.DeleteAlertAsync("user-1", ruleId, Ct)).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAlert(ruleId, Ct);

        result.Should().BeOfType<NoContentResult>();
    }
}
