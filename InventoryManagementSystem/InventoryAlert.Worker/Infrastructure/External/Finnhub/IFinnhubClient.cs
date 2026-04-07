namespace InventoryAlert.Worker.Infrastructure.External.Finnhub;

public interface IFinnhubClient
{
    Task<List<NewsArticle>?> FetchNewsAsync(string symbol, string from, string to, CancellationToken ct);
    Task<FinnhubQuoteModel?> FetchQuoteAsync(string symbol, CancellationToken ct);
}
