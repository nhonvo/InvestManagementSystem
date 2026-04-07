using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace InventoryAlert.Api.Web.Filters;

public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum && schema.Enum != null)
        {
            schema.Enum.Clear();

            foreach (var name in Enum.GetNames(context.Type))
            {
                schema.Enum.Add(JsonValue.Create(name));
            }
        }
    }
}

public class HealthChecksFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument openApiDocument, DocumentFilterContext context)
    {
        // Aligning with the latest Swashbuckle / Microsoft.OpenApi signatures
        openApiDocument.Paths ??= new OpenApiPaths();

        var syntheticPath = new OpenApiPathItem();
        var syntheticOp = new OpenApiOperation
        {
            Summary = "Synthetic health Check",
            Description = "Displays application health status."
        };

        syntheticOp.Responses?.Add("200", new OpenApiResponse { Description = "Healthy" });
        syntheticPath.Operations?.Add(HttpMethod.Get, syntheticOp);

        openApiDocument.Paths?.Add("/synthetic-check", syntheticPath);

        var healthPath = new OpenApiPathItem();
        var healthOp = new OpenApiOperation
        {
            Summary = "Health check Endpoint",
            Description = "Basic system status."
        };

        healthOp.Responses?.Add("200", new OpenApiResponse { Description = "Healthy" });
        healthPath.Operations?.Add(HttpMethod.Get, healthOp);

        openApiDocument.Paths?.Add("/health", healthPath);
    }
}
