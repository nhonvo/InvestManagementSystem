namespace InventoryAlert.Domain.Interfaces;

public interface ICorrelationProvider
{
    string GetCorrelationId();
}

