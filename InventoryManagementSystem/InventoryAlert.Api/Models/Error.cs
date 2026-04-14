using System.Text.Json;

namespace InventoryAlert.Api.Models;

public class Error(string? code = null, string? message = null)
{
    public string? Code { get; set; } = code;
    public string? Message { get; set; } = message;
    public string? Property { get; set; }

    public void AddErrorProperty(ErrorProperty property)
    {
        Property = property.Property;
    }
}

public class ErrorProperty
{
    public string Property { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public ErrorProperty() { }

    public ErrorProperty(string property, string value)
    {
        Property = property;
        Value = value;
    }
}

public class ErrorResponse
{
    public IEnumerable<Error> Errors { get; set; } = [];
    public Guid ErrorId { get; set; } = Guid.NewGuid(); // Keep for tracking

    public ErrorResponse() { }

    public ErrorResponse(IEnumerable<Error> errors)
    {
        Errors = errors;
    }

    public override string ToString() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
}

