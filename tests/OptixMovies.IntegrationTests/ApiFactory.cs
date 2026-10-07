using System.Collections.Concurrent;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OptixMovies.IntegrationTests;

/// <summary>
/// The API, in memory, serving <paramref name="database"/>, with any settings overridden and services replaced.
/// </summary>
public sealed class ApiFactory(
    TestDatabase database,
    IReadOnlyDictionary<string, string>? settings = null,
    Action<IServiceCollection>? replaceServices = null)
    : WebApplicationFactory<Program>
{
    public LogCollector Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Movies", database.ReadOnlyConnectionString);

        // Every test client starts a session, and all of them share one address, so the real limits would soon apply.
        builder.UseSetting("RateLimiting:NewSessionsPerIpPerMinute", "10000");
        builder.UseSetting("RateLimiting:RequestsPerSecond", "10000");
        builder.UseSetting("RateLimiting:Burst", "10000");

        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureTestServices(services => replaceServices?.Invoke(services));
    }
}

/// <summary>Keeps every log message, so tests can check what the API reported.</summary>
public sealed class LogCollector : ILoggerProvider
{
    private readonly ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> entries = new();

    public IReadOnlyList<string> Warnings =>
        [.. entries.Where(entry => entry.Level == LogLevel.Warning).Select(entry => entry.Message)];

    public IReadOnlyList<Exception?> LoggedErrors =>
        [.. entries.Where(entry => entry.Level >= LogLevel.Error).Select(entry => entry.Exception)];

    public ILogger CreateLogger(string categoryName) => new Logger(entries);

    public void Dispose()
    {
    }

    private sealed class Logger(ConcurrentQueue<(LogLevel, string, Exception?)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue((logLevel, formatter(state, exception), exception));
    }
}
