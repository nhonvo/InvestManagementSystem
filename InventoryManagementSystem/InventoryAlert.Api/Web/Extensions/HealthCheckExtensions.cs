using InventoryAlert.Api.Domain.Constants;
using InventoryAlert.Api.Web.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InventoryAlert.Api.Web.Extensions;

public static class HealthCheckExtensions
{
    public static void SetupHealthCheck(this IServiceCollection services, AppSettings configuration)
    {
        var healthCheckBuilder = services.AddHealthChecks();

        // Database check (Npgsql)
        //healthCheckBuilder.AddNpgsql(
        //    connectionString: configuration.Database.DefaultConnection,
        //        name: HealthCheck.DBHealthCheck,
        //        failureStatus: HealthStatus.Unhealthy,
        //        tags: new[] { HealthCheck.InfrastructureCheck }
        //);

        // PostgREST admin port check
        healthCheckBuilder.AddUrlGroup(
            uri: new Uri("http://localhost:3001/ready"),
            name: "PostgREST",
            failureStatus: HealthStatus.Degraded,
            tags: new[] { "api", "infrastructure" }
        );

        // services.AddHealthChecksUI().AddInMemoryStorage(); // Requires UI packages
    }

    public static void ConfigureHealthCheck(this WebApplication app)
    {
        app.UseHealthChecks("/health", new HealthCheckOptions
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

        // Additional endpoint for external synthetic checks
        app.UseHealthChecks("/synthetic-check", new HealthCheckOptions
        {
            Predicate = check =>
                check.Tags.Contains(HealthCheck.InfrastructureCheck) ||
                check.Tags.Contains(HealthCheck.ExternalServiceCheck) ||
                check.Tags.Contains("api")
        });
    }
}
