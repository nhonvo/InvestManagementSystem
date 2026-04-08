using System.Text.Json.Serialization;
using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using InventoryAlert.Api.Web.Filters;
using InventoryAlert.Api.Web.Utilities;
using InventoryAlert.Api.Web.Validations;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAlert.Api.Web.Extensions;

public static class MvcExtension
{
    public static void SetupMvc(this IServiceCollection services)
    {
        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.ReportApiVersions = true;
            opt.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(opt =>
        {
            opt.GroupNameFormat = "'v'V";
            opt.SubstituteApiVersionInUrl = true;
        });

        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

        services.AddControllers(options => options.Filters.Add(typeof(ValidateModelFilter)))
            .AddJsonOptions(options =>
            {
                // Ignore null values
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

                // Avoid reference loop issues
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

                // Use ISO 8601 format for DateTime and DateTimeOffset
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonDateTimeOffsetConverter());

                // null strings → "" | null numbers → 0 (using custom converters)
                // options.JsonSerializerOptions.Converters.Add(new NullToDefaultConverter());
                options.JsonSerializerOptions.Converters.Add(new TrimmingConverter());
                options.JsonSerializerOptions.Converters.Add(new DecimalPrecisionConverter(2));
            });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddTransient<IValidatorInterceptor, ValidatorInterceptor>();
    }
}
