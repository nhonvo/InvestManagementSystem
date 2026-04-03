using InventoryAlert.Api.Infrastructure.External.Models;

namespace InventoryAlert.Api.Infrastructure.External.Interfaces
{
    public interface IFinnhubClient
    {
        public Task<FinnhubQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
    }
}
