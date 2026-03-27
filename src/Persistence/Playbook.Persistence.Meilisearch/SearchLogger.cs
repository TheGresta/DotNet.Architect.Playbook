namespace Playbook.Persistence.Meilisearch;

/// <summary>
/// Provides high-performance, structured logging for search operations.
/// Utilizing .NET's Source-Generated LoggerMessage pattern, this class ensures 
/// minimal allocation and maximum throughput when recording search telemetry.
/// </summary>
/// <remarks>
/// By using a partial class and the [LoggerMessage] attribute, the compiler generates 
/// optimized code that avoids the overhead of string formatting and object boxing 
/// typically associated with standard logging methods.
/// </remarks>
public partial class SearchLogger(ILogger<SearchLogger> logger)
{
    /// <summary>
    /// Logs performance metrics for a completed search operation.
    /// Includes a pre-flight check for log level enablement to bypass 
    /// telemetry overhead in production environments where Information-level 
    /// logging might be disabled.
    /// </summary>
    /// <param name="term">The raw search query string provided by the user.</param>
    /// <param name="elapsedMs">The duration of the search engine operation in milliseconds.</param>
    /// <param name="count">The total number of hits returned by the engine.</param>
    public void LogSearchPerformance(string? term, long elapsedMs, long count)
    {
        // Performance optimization: Avoid executing the generated core logic if 
        // the Information log level is not currently being captured.
        if (!logger.IsEnabled(LogLevel.Information)) return;

        LogSearchPerformanceCore(logger, term, elapsedMs, count);
    }

    /// <summary>
    /// The source-generated core implementation for structured logging.
    /// This method is compiled into a highly efficient, non-allocating log call.
    /// </summary>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Search executed for '{Term}' in {ElapsedMs}ms. Results: {Count}")]
    static partial void LogSearchPerformanceCore(ILogger logger, string? term, long elapsedMs, long count);
}
