using Microsoft.Extensions.Logging;

namespace TodoApi.Tests;

// TestLoggerProviderは、テスト中に出力されたログをメモリへ保存します。
public sealed class TestLoggerProvider : ILoggerProvider
{
    public List<string> Messages { get; } = [];
    public List<EventId> Events { get; } = [];

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(Messages, Events);
    }

    public void Dispose()
    {
    }

    private sealed class TestLogger(List<string> messages, List<EventId> events) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            lock (messages)
            {
                messages.Add(formatter(state, exception));
                events.Add(eventId);
            }
        }
    }
}
