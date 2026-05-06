using System.Net;
using FluentAssertions;
using InventoryAlert.IntegrationTests.Abstractions;
using InventoryAlert.IntegrationTests.Infrastructure;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using RestSharp;
using Xunit.Abstractions;
using Xunit;

namespace InventoryAlert.IntegrationTests.Tests.Api;

[Trait("Category", "Api")]
public class AuthApiTest : Tier2TestBase
{
    public AuthApiTest(TestFixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
    }

    [Fact]
    
    public async Task Login_ShouldReturnAccessToken_AndLogSuccess()
    {
        // Arrange
        var username = "admin_test_" + Guid.NewGuid().ToString().Substring(0, 8);
        var password = "password";

        // Seed user directly in DB
        var uow = Services.GetRequiredService<IUnitOfWork>();
        var user = new User
        {
            Username = username,
            Email = $"{username}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Admin"
        };
        await uow.Users.AddAsync(user, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await RunAction(async () => {
            var request = new RestRequest("auth/login", Method.Post);
            request.AddJsonBody(new { username, password });
            return await Client.ExecuteAsync<AuthResponse>(request);
        });

        // Assert
        result.Response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Response.Data.Should().NotBeNull();
        
        // Log Verification (Seq provides robust filtering)
        result.Logs.Should().Contain(l => l.Contains(username), because: "Successful login should be logged with the username in Seq.");
    }
}
