using System.Text.Json;
using InventoryAlert.Domain.Configuration;
using InventoryAlert.Domain.External.Finnhub;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RestSharp;

namespace InventoryAlert.Infrastructure.External.Finnhub;

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

    public async Task<FinnhubMetricsResponse?> GetMetricsAsync(string symbol, CancellationToken ct = default)
        => await GetAsync<FinnhubMetricsResponse>("stock/metric", req =>
        {
            req.AddParameter("symbol", symbol);
            req.AddParameter("metric", "all");
        }, ct);

    public async Task<FinnhubInsiderResponse?> GetInsidersAsync(string symbol, CancellationToken ct = default)
        => await GetAsync<FinnhubInsiderResponse>("stock/insider-transactions", req =>
        {
            req.AddParameter("symbol", symbol);
        }, ct);

    public async Task<FinnhubIpoCalendar?> GetIpoCalendarAsync(string from, string to, CancellationToken ct = default)
        => await GetAsync<FinnhubIpoCalendar>("calendar/ipo", req =>
        {
            req.AddParameter("from", from);
            req.AddParameter("to", to);
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

            if (!res.IsSuccessful)
            {
                // Silently skip or downgrading logging for plan restrictions (401/403 Access Denied)
                // This avoids log pollution when using a Free Tier key for premium endpoints
                if ((status == 401 || status == 403) && (res.Content?.Contains("access to this resource") == true || res.Content?.Contains("don't have access") == true))
                {
                    _logger.LogInformation("[Finnhub] Plan restriction on {Path} for status {Status}. Skip.", path, status);
                }
                else
                {
                    _logger.LogWarning("[Finnhub] {Path} failed with status {Status}: {ErrorMessage}. Content: {Content}",
                        path, status, res.ErrorMessage, res.Content);
                }

                if (status >= 500 || status == 429)
                {
                    throw new HttpRequestException($"[Finnhub] Transient failure {status}");
                }
            }

            return res;
        });

        return response.Data;
    }

    private async Task<List<T>> GetListAsync<T>(string path, Action<RestRequest> configure, CancellationToken ct)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            var req = new RestRequest(path, Method.Get);
            req.AddParameter("token", _token);
            configure(req);

            var res = await _client.ExecuteAsync(req, ct);
            var status = (int)res.StatusCode;

            if (!res.IsSuccessful)
            {
                if ((status == 401 || status == 403) && (res.Content?.Contains("access to this resource") == true || res.Content?.Contains("don't have access") == true))
                {
                    _logger.LogInformation("[Finnhub] Plan restriction on {Path} for status {Status}. Skip.", path, status);
                }
                else
                {
                    _logger.LogWarning("[Finnhub] {Path} failed with status {Status}: {ErrorMessage}. Content: {Content}",
                        path, status, res.ErrorMessage, res.Content);
                }

                if (status >= 500 || status == 429)
                {
                    throw new HttpRequestException($"[Finnhub] Transient failure {status}");
                }

                return new RestResponse<List<T>>(req) { StatusCode = res.StatusCode, ResponseStatus = res.ResponseStatus };
            }

            try
            {
                var data = JsonSerializer.Deserialize<List<T>>(res.Content ?? "[]", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new RestResponse<List<T>>(req) { Data = data, StatusCode = res.StatusCode, ResponseStatus = ResponseStatus.Completed };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[Finnhub] {Path} deserialization failed. Content: {Content}", path, res.Content);
                return new RestResponse<List<T>>(req) { StatusCode = res.StatusCode, ResponseStatus = ResponseStatus.Error };
            }
        });

        return response.Data ?? [];
    }
}
