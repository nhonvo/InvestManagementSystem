using InventoryAlert.Domain.Common.Constants;
using InventoryAlert.Domain.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using HealthChecks.NpgSql;

namespace InventoryAlert.Worker.Extensions;

public static class HealthCheckExtensions
{
    public static void SetupHealthCheck(this IServiceCollection services, AppSettings configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.Database.DefaultConnection,
                name: HealthCheck.DBHealthCheck,
                tags: new[] { HealthCheck.InfrastructureCheck, "db", "worker" });
    }

    public static void ConfigureHealthCheck(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration
                    }),
                    totalDuration = report.TotalDuration
                };
                await context.Response.WriteAsJsonAsync(response);
            }
        });
    }
}
