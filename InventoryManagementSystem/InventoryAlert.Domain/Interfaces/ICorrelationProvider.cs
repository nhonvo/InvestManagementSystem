namespace InventoryAlert.Domain.Interfaces;

public interface ICorrelationProvider
{
    string GetCorrelationId();
    void SetCorrelationId(string correlationId);
}

