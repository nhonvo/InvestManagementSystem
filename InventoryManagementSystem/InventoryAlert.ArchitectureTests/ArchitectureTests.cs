using NetArchTest.Rules;
using Xunit;

namespace InventoryAlert.ArchitectureTests;

public class ArchitectureTests
{
    private const string ApiNamespace = "InventoryAlert.Api";
    private const string WorkerNamespace = "InventoryAlert.Worker";
    private const string ContractsNamespace = "InventoryAlert.Contracts";

    [Fact]
    public void Api_Should_Not_Have_Internal_Entities()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Web.Controllers.EventsController).Assembly;
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Api.Domain.Entities")
            .ShouldNot()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, "Api project contains internal entity classes. Move them to Contracts.");
    }

    [Fact]
    public void Worker_Should_Not_Have_Internal_Entities()
    {
        // Fixed anchor: NewsHandler is now in Application.IntegrationHandlers
        var workerAssembly = typeof(InventoryAlert.Worker.Application.IntegrationHandlers.NewsHandler).Assembly;
        var result = Types.InAssembly(workerAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Worker.Entities")
            .ShouldNot()
            .BeClasses()
            .GetResult();

        Assert.True(result.IsSuccessful, "Worker project contains internal entity classes. Move them to Contracts.");
    }

    [Fact]
    public void Application_Should_Not_DependOn_Infrastructure()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Web.Controllers.EventsController).Assembly;
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Api.Application")
            .ShouldNot()
            .HaveDependencyOn("InventoryAlert.Api.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "Application layer shouldn't depend on Infrastructure layer.");
    }

    [Fact]
    public void Controllers_Should_Inherit_ControllerBase_And_EndWith_Controller()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Web.Controllers.EventsController).Assembly;
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
    public void WebLayer_Should_Not_DependOn_Infrastructure()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Web.Controllers.EventsController).Assembly;
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Api.Web.Controllers")
            .ShouldNot()
            .HaveDependencyOn("InventoryAlert.Api.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "Controllers shouldn't bypass Application layer to strictly access Infrastructure.");
    }

    [Fact]
    public void ApplicationServices_Should_HaveNameEndingWith_Service()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Web.Controllers.EventsController).Assembly;
        var result = Types.InAssembly(apiAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Api.Application.Services")
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
        // Fixed namespace: Application.IntegrationHandlers
        var workerAssembly = typeof(InventoryAlert.Worker.Application.IntegrationHandlers.NewsHandler).Assembly;
        var result = Types.InAssembly(workerAssembly)
            .That()
            .ResideInNamespace("InventoryAlert.Worker.Application.IntegrationHandlers")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(result.IsSuccessful, "Worker handlers must end with 'Handler'");
    }

    [Fact]
    public void Contracts_Should_Be_Independent()
    {
        var contractsAssembly = typeof(InventoryAlert.Contracts.Entities.Product).Assembly;
        var result = Types.InAssembly(contractsAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, "Contracts (Domain layer) should not depend on ASP.NET Core.");
    }

    [Fact]
    public void Repository_Methods_Should_Be_Async()
    {
        var apiAssembly = typeof(InventoryAlert.Api.Web.Controllers.EventsController).Assembly;
        var repositoryInterfaces = apiAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository"));

        foreach (var repo in repositoryInterfaces)
        {
            foreach (var method in repo.GetMethods())
            {
                Assert.True(
                    typeof(Task).IsAssignableFrom(method.ReturnType),
                    $"Method {method.Name} in {repo.Name} must return a Task or Task<T>. Synchronous I/O is blocked."
                );
            }
        }
    }
}
