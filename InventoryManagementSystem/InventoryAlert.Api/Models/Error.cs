using System.Text.Json;

namespace InventoryAlert.Api.Models;

public class Error(string code, string message, string? property = null)
{
    public string Code { get; set; } = code;
    public string Message { get; set; } = message;
    public string? Property { get; set; } = property;
}

public class ErrorResponse
{
    public IEnumerable<Error> Errors { get; set; } = [];
    public Guid ErrorId { get; set; } = Guid.NewGuid();
    public string? CorrelationId { get; set; }

    public ErrorResponse() { }

    public ErrorResponse(IEnumerable<Error> errors, string? correlationId = null)
    {
        Errors = errors;
        CorrelationId = correlationId;
    }

    public ErrorResponse(Error error, string? correlationId = null)
    {
        Errors = [error];
        CorrelationId = correlationId;
    }

    public override string ToString() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
}
