using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Services;

namespace InventoryAlert.Api.Web.ServiceExtensions;

public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers Application-layer services (use-cases / orchestrators).
    /// Nothing in here should reference EF Core or any infrastructure concern.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
