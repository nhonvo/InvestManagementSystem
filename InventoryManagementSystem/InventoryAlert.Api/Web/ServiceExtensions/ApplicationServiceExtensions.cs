using FluentValidation;
using FluentValidation.AspNetCore;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Services;
using InventoryAlert.Api.Application.Validators;

namespace InventoryAlert.Api.Web.ServiceExtensions;

public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers Application-layer services.
    /// No EF Core or infrastructure imports allowed here.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IWatchlistService, WatchlistService>();
        services.AddScoped<IAlertRuleService, AlertRuleService>();
        services.AddScoped<IStockDataService, StockDataService>();
        services.AddScoped<IAuthService, AuthService>();

        // Validation rules are registered here, but auto-validation execution
        // is plugged alongside MVC in MvcExtension to avoid duplication.
        services.AddValidatorsFromAssemblyContaining<ProductRequestValidator>();

        // Add Memory Cache for ProductService caching
        services.AddMemoryCache();

        return services;
    }
}
