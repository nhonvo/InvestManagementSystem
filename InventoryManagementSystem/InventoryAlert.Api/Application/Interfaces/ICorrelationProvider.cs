namespace InventoryAlert.Api.Application.Interfaces;

public interface ICorrelationProvider
{
    string GetCorrelationId();
}
