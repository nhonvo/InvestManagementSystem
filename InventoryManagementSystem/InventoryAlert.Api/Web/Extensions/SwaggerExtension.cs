using InventoryAlert.Api.Web.Configuration;
using InventoryAlert.Api.Web.Filters;
using InventoryAlert.Contracts.Common.Constants;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace InventoryAlert.Api.Web.Extensions;

public static class SwaggerExtension
{
    private static readonly List<string> BearerValues = ["Bearer"];

    public static IServiceCollection AddSwaggerOpenAPI(this IServiceCollection services, AppSettings appSettings)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = $"{ApplicationConstants.Name} API",
                Version = "v1",
                Description = "Modern Inventory Management System with Event-Driven Stock Alerts",
                Contact = new OpenApiContact
                {
                    Email = "vothuongtruongnhon2002@gmail.com",
                    Name = "Truong Nhon",
                    Url = new Uri("https://github.com/vothuongtruongnhon")
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });

            // XML Documentation support
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

            // JWT Security Definition
            var securityScheme = new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            options.AddSecurityDefinition("Bearer", securityScheme);

            // Using the delegating Func overload for the new Microsoft.OpenApi model
            options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", doc),
                    BearerValues
                }
            });

            options.SchemaFilter<EnumSchemaFilter>();
            options.DocumentFilter<HealthChecksFilter>();
        });

        return services;
    }

    public static void UseSwaggerWithUI(this IApplicationBuilder app)
    {
        app.UseSwagger(c =>
        {
            c.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(setupAction =>
        {
            setupAction.SwaggerEndpoint("/swagger/v1/swagger.json", $"{ApplicationConstants.Name} API v1");
            setupAction.RoutePrefix = "swagger";
        });

        if (app is IEndpointRouteBuilder endpoints)
        {
            endpoints.MapScalarApiReference(options =>
            {
                options
                    .WithTitle($"{ApplicationConstants.Name} API Reference")
                    .WithTheme(ScalarTheme.Mars)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }
    }
}
