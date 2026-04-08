using InventoryAlert.Api.Application.DTOs;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Web.Configuration;
using Polly;
using Polly.Retry;
using RestSharp;
using System.Net.Http;

namespace InventoryAlert.Api.Infrastructure.External;

public class FinnhubClient(
    RestClient restClient,
    AppSettings appSettings,
    ILogger<FinnhubClient> logger) : IFinnhubClient
{
    private readonly RestClient _client = restClient;
    private readonly string _token = appSettings.Finnhub.ApiKey ?? string.Empty;
    private readonly ILogger<FinnhubClient> _logger = logger;
    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    public async Task<FinnhubQuoteResponse?> GetQuoteAsync(string symbol, CancellationToken ct = default)
        => await GetAsync<FinnhubQuoteResponse>("quote", req =>
        {
            req.AddParameter("symbol", symbol);
        }, ct);

    public async Task<FinnhubProfileResponse?> GetProfileAsync(string symbol, CancellationToken ct = default)
        => await GetAsync<FinnhubProfileResponse>("stock/profile2", req =>
        {
            req.AddParameter("symbol", symbol);
        }, ct);

    public async Task<List<FinnhubNewsItem>> GetCompanyNewsAsync(string symbol, string from, string to, CancellationToken ct = default)
        => await GetListAsync<FinnhubNewsItem>("company-news", req =>
        {
            req.AddParameter("symbol", symbol);
            req.AddParameter("from", from);
            req.AddParameter("to", to);
        }, ct);

    public async Task<List<FinnhubRecommendation>> GetRecommendationsAsync(string symbol, CancellationToken ct = default)
        => await GetListAsync<FinnhubRecommendation>("stock/recommendation", req =>
        {
            req.AddParameter("symbol", symbol);
        }, ct);

    public async Task<List<FinnhubEarnings>> GetEarningsAsync(string symbol, CancellationToken ct = default)
        => await GetListAsync<FinnhubEarnings>("stock/earnings", req =>
        {
            req.AddParameter("symbol", symbol);
        }, ct);

    public async Task<List<string>> GetPeersAsync(string symbol, CancellationToken ct = default)
        => await GetListAsync<string>("stock/peers", req =>
        {
            req.AddParameter("symbol", symbol);
        }, ct);

    public async Task<List<FinnhubNewsItem>> GetMarketNewsAsync(string category, CancellationToken ct = default)
        => await GetListAsync<FinnhubNewsItem>("news", req =>
        {
            req.AddParameter("category", category);
        }, ct);

    public async Task<FinnhubMarketStatus?> GetMarketStatusAsync(string exchange, CancellationToken ct = default)
        => await GetAsync<FinnhubMarketStatus>("stock/market-status", req =>
        {
            req.AddParameter("exchange", exchange);
        }, ct);

    public async Task<List<FinnhubHoliday>> GetMarketHolidaysAsync(string exchange, CancellationToken ct = default)
        => await GetListAsync<FinnhubHoliday>("stock/market-holiday", req =>
        {
            req.AddParameter("exchange", exchange);
        }, ct);

    public async Task<FinnhubEarningsCalendar?> GetEarningsCalendarAsync(string from, string to, CancellationToken ct = default)
        => await GetAsync<FinnhubEarningsCalendar>("calendar/earnings", req =>
        {
            req.AddParameter("from", from);
            req.AddParameter("to", to);
        }, ct);

    public async Task<FinnhubSymbolSearch?> SearchSymbolsAsync(string query, CancellationToken ct = default)
        => await GetAsync<FinnhubSymbolSearch>("search", req =>
        {
            req.AddParameter("q", query);
        }, ct);

    public async Task<List<string>> GetCryptoExchangesAsync(CancellationToken ct = default)
        => await GetListAsync<string>("crypto/exchange", _ => { }, ct);

    public async Task<List<FinnhubCryptoSymbol>> GetCryptoSymbolsAsync(string exchange, CancellationToken ct = default)
        => await GetListAsync<FinnhubCryptoSymbol>("crypto/symbol", req =>
        {
            req.AddParameter("exchange", exchange);
        }, ct);

    // ── Shared helpers ────────────────────────────────────────────────────────

    private async Task<T?> GetAsync<T>(string path, Action<RestRequest> configure, CancellationToken ct)
        where T : class
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            var req = new RestRequest(path, Method.Get);
            req.AddParameter("token", _token);
            configure(req);

            var res = await _client.ExecuteAsync<T>(req, ct);
            var status = (int)res.StatusCode;
            
            if (!res.IsSuccessful && (status >= 500 || res.ResponseStatus == ResponseStatus.Error || res.ResponseStatus == ResponseStatus.TimedOut))
            {
                throw res.ErrorException ?? new HttpRequestException($"[Finnhub] {path} failure: {status}");
            }
            
            return res;
        });

        if (response.IsSuccessful) return response.Data;

        _logger.LogError("[Finnhub] {Path} failed: {Error}", path, response.ErrorMessage);
        return null;
    }

    private async Task<List<T>> GetListAsync<T>(string path, Action<RestRequest> configure, CancellationToken ct)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            var req = new RestRequest(path, Method.Get);
            req.AddParameter("token", _token);
            configure(req);

            var res = await _client.ExecuteAsync<List<T>>(req, ct);
            var status = (int)res.StatusCode;
            
            if (!res.IsSuccessful && (status >= 500 || res.ResponseStatus == ResponseStatus.Error || res.ResponseStatus == ResponseStatus.TimedOut))
            {
                throw res.ErrorException ?? new HttpRequestException($"[Finnhub] {path} failure: {status}");
            }
            
            return res;
        });

        if (response.IsSuccessful && response.Data is not null) return response.Data;

        _logger.LogError("[Finnhub] {Path} failed: {Error}", path, response.ErrorMessage);
        return [];
    }
}
