namespace Playbook.Persistence.Meilisearch;

public partial class SearchLogger(ILogger<SearchLogger> logger)
{
    public void LogSearchPerformance(string? term, long elapsedMs, long count)
    {
        if (!logger.IsEnabled(LogLevel.Information)) return;

        LogSearchPerformanceCore(logger, term, elapsedMs, count);
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Search executed for '{Term}' in {ElapsedMs}ms. Results: {Count}")]
    static partial void LogSearchPerformanceCore(ILogger logger, string? term, long elapsedMs, long count);
}
