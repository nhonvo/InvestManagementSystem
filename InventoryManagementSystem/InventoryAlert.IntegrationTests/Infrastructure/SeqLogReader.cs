using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace InventoryAlert.IntegrationTests.Infrastructure;

public class SeqLogReader
{
    private readonly HttpClient _httpClient;

    public SeqLogReader(string seqUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(seqUrl) };
    }

    public async Task<List<string>> GetLogsByCorrelationIdAsync(string correlationId, int count = 100, CancellationToken ct = default)
    {
        for (int i = 0; i < 6; i++)
        {
            try
            {
                var filter = $"CorrelationId = '{correlationId}'";
                var url = $"/api/events?count={count}&filter={Uri.EscapeDataString(filter)}";

                var response = await _httpClient.GetFromJsonAsync<List<SeqEventResponse>>(url, ct);
                
                if (response != null && response.Any())
                {
                    return response.Select(e => $"[{e.Level}] {e.RenderMessage()}").ToList();
                }
            }
            catch { }
            await Task.Delay(2000, ct);
        }
        return new List<string>();
    }

    public async Task<List<string>> GetRecentLogsAsync(int count = 50, CancellationToken ct = default)
    {
        try
        {
            var url = $"/api/events?count={count}";
            var response = await _httpClient.GetFromJsonAsync<List<SeqEventResponse>>(url, ct);
            return response?.Select(e => $"[{e.Level}] {e.RenderMessage()}").ToList() ?? new List<string>();
        }
        catch { return new List<string>(); }
    }

    public async Task<bool> WaitForLogFragmentAsync(string fragment, int timeoutSeconds = 30, int tailCount = 200)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < TimeSpan.FromSeconds(timeoutSeconds))
        {
            var logs = await GetRecentLogsAsync(tailCount);
            if (logs.Any(l => l.Contains(fragment))) return true;
            await Task.Delay(2000);
        }
        return false;
    }

    private class SeqEventResponse
    {
        [JsonPropertyName("Level")]
        public string? Level { get; set; }

        [JsonPropertyName("MessageTemplateTokens")]
        public List<MessageToken>? MessageTemplateTokens { get; set; }

        [JsonPropertyName("Properties")]
        public List<SeqProperty>? Properties { get; set; }

        public string RenderMessage()
        {
            if (MessageTemplateTokens == null) return string.Empty;
            
            var sb = new System.Text.StringBuilder();
            foreach (var token in MessageTemplateTokens)
            {
                if (token.Text != null) sb.Append(token.Text);
                else if (token.PropertyName != null)
                {
                    var prop = Properties?.FirstOrDefault(p => p.Name == token.PropertyName);
                    sb.Append(prop?.Value?.ToString() ?? $"{{{token.PropertyName}}}");
                }
            }
            return sb.ToString();
        }
    }

    private class MessageToken
    {
        [JsonPropertyName("Text")]
        public string? Text { get; set; }

        [JsonPropertyName("PropertyName")]
        public string? PropertyName { get; set; }
    }

    private class SeqProperty
    {
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Value")]
        public object? Value { get; set; }
    }
}
