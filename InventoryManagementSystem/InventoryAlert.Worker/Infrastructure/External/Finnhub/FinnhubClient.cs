using RestSharp;

namespace InventoryAlert.Worker.Infrastructure.External.Finnhub;

public class FinnhubClient(RestClient restClient, ILogger<FinnhubClient> logger) : IFinnhubClient
{
    private readonly RestClient _restClient = restClient;
    private readonly ILogger<FinnhubClient> _logger = logger;


    public async Task<List<NewsArticle>?> FetchNewsAsync(string symbol, string from, string to, CancellationToken ct)
    {
        try
        {
            var req = new RestRequest("company-news")
                .AddParameter("symbol", symbol)
                .AddParameter("from", from)
                .AddParameter("to", to);
            var resp = await _restClient.ExecuteAsync<List<NewsArticle>>(req, ct);
            return resp.IsSuccessful ? resp.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FinnhubClient] Finnhub news call failed for {Symbol}.", symbol);
            return null;
        }
    }

    public async Task<FinnhubQuoteModel?> FetchQuoteAsync(string symbol, CancellationToken ct)
    {
        try
        {
            var req = new RestRequest("quote").AddParameter("symbol", symbol);
            var result = await _restClient.ExecuteAsync<FinnhubQuoteModel>(req, ct);
            return result.IsSuccessful ? result.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FinnhubClient] Finnhub quote call failed for {Symbol}.", symbol);
            return null;
        }
    }
}
