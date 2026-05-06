using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace InventoryAlert.IntegrationTests.Infrastructure;

public record LogEntry(LogLevel Level, string Message, Exception? Exception, string Category);

public class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentBag<LogEntry> _entries = new();

    public IEnumerable<LogEntry> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new TestLogger(this, categoryName);

    public void Dispose() { }

    public void Clear() => _entries.Clear();

    private class TestLogger(TestLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            provider._entries.Add(new LogEntry(logLevel, formatter(state, exception), exception, category));
        }
    }
}
