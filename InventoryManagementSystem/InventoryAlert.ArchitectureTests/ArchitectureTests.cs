using InventoryAlert.Worker.IntegrationEvents.Handlers;
using NetArchTest.Rules;

namespace InventoryAlert.ArchitectureTests;

public class ArchitectureTests
{
    private const string ApiNamespace = "InventoryAlert.Api";
    private const string WorkerNamespace = "InventoryAlert.Worker";
    private const string DomainNamespace = "InventoryAlert.Domain";

    [Fact]
    public void Api_Should_Not_Have_Internal_Entities()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Controllers.AuthController).Assembly;
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Domain.Entities.Postgres")
            .ShouldNot()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, "Api project contains internal entity classes. Move them to Domain.");
    }

    [Fact]
    public void Worker_Should_Not_Have_Internal_Entities()
    {
        var workerAssembly = typeof(LowHoldingsHandler).Assembly;
        var result = Types.InAssembly(workerAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Worker.Entities")
            .ShouldNot()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, "Worker project contains internal entity classes. Move them to Domain.");
    }

    [Fact]
    public void ApiServices_Should_Be_Independent_Of_Infrastructure_Logic()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Controllers.AuthController).Assembly;
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Api.Services")
            .ShouldNot()
            .HaveDependencyOn("InventoryAlert.Infrastructure.Messaging")
            .GetResult();

        Assert.True(result.IsSuccessful, "API Business services shouldn't directly depend on specialized messaging infra.");
    }

    [Fact]
    public void Controllers_Should_Inherit_ControllerBase_And_EndWith_Controller()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Controllers.AuthController).Assembly;
        var result = Types.InAssembly(apiAssembly)
            .That()
            .Inherit(typeof(Microsoft.AspNetCore.Mvc.ControllerBase))
            .Should()
            .HaveNameEndingWith("Controller")
            .And()
            .ResideInNamespaceEndingWith("Controllers")
            .GetResult();

        Assert.True(result.IsSuccessful, "Controllers must end with 'Controller' and reside in Controllers namespace");
    }

    [Fact]
    public void ApplicationServices_Should_HaveNameEndingWith_Service()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Controllers.AuthController).Assembly;
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Api.Services")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Service")
            .GetResult();

        Assert.True(result.IsSuccessful, "Application layer services must end with 'Service'");
    }

    [Fact]
    public void WorkerHandlers_Should_EndWith_Handler()
    {
        var workerAssembly = typeof(LowHoldingsHandler).Assembly;
        var result = Types.InAssembly(workerAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Worker.IntegrationEvents.Handlers")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(result.IsSuccessful, "Worker handlers must end with 'Handler'");
    }

    [Fact]
    public void Domain_Should_Be_Independent()
    {
        var domainAssembly = typeof(InventoryAlert.Domain.Entities.Postgres.StockListing).Assembly;
        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .And()
            .HaveDependencyOn("InventoryAlert.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "Domain layer must be pure and independent of Infrastructure/Api.");
    }

    [Fact]
    public void Repository_Methods_Is_Async_In_Interfaces()
    {
        var domainAssembly = typeof(InventoryAlert.Domain.Entities.Postgres.StockListing).Assembly;
        var repositoryInterfaces = domainAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository"));

        foreach (var repo in repositoryInterfaces)
        {
            foreach (var method in repo.GetMethods())
            {
                Assert.True(
                    typeof(Task).IsAssignableFrom(method.ReturnType),
                    $"Method {method.Name} in {repo.Name} must return a Task or Task<T>."
                );
            }
        }
    }
}

