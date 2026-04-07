using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Configuration;
using RestSharp;

namespace InventoryAlert.Api.Infrastructure.External;

public class FinnhubClient(RestClient restClient, AppSettings appSettings, ILogger<FinnhubClient> logger) : IFinnhubClient
{
    private readonly RestClient _restClient = restClient;
    private readonly AppSettings _appSettings = appSettings;
    private readonly ILogger<FinnhubClient> _logger = logger;

    public async Task<FinnhubQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var request = new RestRequest("quote", Method.Get);
        request.AddParameter("symbol", symbol);
        request.AddParameter("token", _appSettings.Finnhub.ApiKey);

        var response = await _restClient.ExecuteAsync<FinnhubQuoteResponse>(request, cancellationToken);

        if (response.IsSuccessful)
            return response.Data;

        _logger.LogError("[FinnhubClient] Error fetching quote for {Symbol}: {ErrorMessage}", symbol, response.ErrorMessage);
        return null;
    }
}
