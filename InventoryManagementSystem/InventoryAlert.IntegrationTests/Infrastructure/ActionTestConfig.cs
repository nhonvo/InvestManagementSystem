using RestSharp;

namespace InventoryAlert.IntegrationTests.Infrastructure;

public record TestResult<T>(RestResponse<T> Response, List<string> Logs);

public class ActionTestConfig(SeqLogReader logReader)
{
    public async Task<TestResult<T>> RunActionAndViewLog<T>(Func<Task<RestResponse<T>>> action)
    {
        var response = await action();
        
        var correlationId = response.Headers?
            .FirstOrDefault(h => h.Name != null && h.Name.Equals("X-Correlation-Id", StringComparison.OrdinalIgnoreCase))?
            .Value?.ToString();

        if (string.IsNullOrEmpty(correlationId))
        {
            return new TestResult<T>(response, new List<string> { "[Warning]: X-Correlation-Id header missing from response." });
        }

        // Fetch logs from Seq
        var logs = await logReader.GetLogsByCorrelationIdAsync(correlationId);
        
        return new TestResult<T>(response, logs);
    }
}
