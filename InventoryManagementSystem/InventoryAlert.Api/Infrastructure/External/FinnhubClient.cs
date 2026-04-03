using InventoryAlert.Api.Infrastructure.External.Interfaces;
using InventoryAlert.Api.Infrastructure.External.Models;
using InventoryAlert.Api.Web.Configuration;
using RestSharp;

namespace InventoryAlert.Api.Infrastructure.External
{
    public class FinnhubClient(RestClient restClient, AppSettings appSettings) : IFinnhubClient
    {
        private readonly RestClient _restClient = restClient;
        private readonly AppSettings _appSettings = appSettings;

        public async Task<FinnhubQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
        {
            var request = new RestRequest("quote", Method.Get);
            request.AddParameter("symbol", symbol);
            request.AddParameter("token", _appSettings.Finnhub.ApiKey);

            var response = await _restClient.ExecuteAsync<FinnhubQuoteResponse>(request, cancellationToken);

            if (response.IsSuccessful)
                return response.Data;

            Console.WriteLine($"[FinnhubClient] Error fetching quote for {symbol}: {response.ErrorMessage}");
            return null;
        }
    }
}
