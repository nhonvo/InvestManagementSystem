using InventoryAlert.Api.Application.DTOs;

namespace InventoryAlert.Api.Application.Interfaces;

public interface IFinnhubClient
{
    public Task<FinnhubQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
}
