namespace Playbook.Messaging.MassTransit.Courier.Messaging;

/// <summary>
/// Defines a contract for introducing controlled instability into a workflow to test resilience and compensation logic.
/// </summary>
public interface IChaosProvider
{
    /// <summary>
    /// Evaluates system stability for a specific activity. Throws an exception if a failure is simulated.
    /// </summary>
    /// <param name="activityName">The name of the calling activity for logging and identification purposes.</param>
    /// <exception cref="ChaosException">Thrown when a simulated failure is triggered.</exception>
    void EnsureStability(string activityName);
}

/// <summary>
/// Exception thrown by the <see cref="IChaosProvider"/> to simulate a transient or permanent system failure.
/// </summary>
/// <param name="message">The error message describing the simulated failure context.</param>
public sealed class ChaosException(string message) : Exception(message);

/// <summary>
/// A concrete implementation of <see cref="IChaosProvider"/> that uses a pseudo-random generator to simulate high-frequency failures.
/// </summary>
public sealed class ChaosProvider(ILogger<ChaosProvider> logger) : IChaosProvider
{
    private readonly Random _random = new();

    /// <summary>
    /// Performs a non-deterministic stability check. 
    /// Currently configured with a 50% failure rate to aggressively test MassTransit Courier's compensation segments.
    /// </summary>
    /// <param name="activityName">The name of the activity currently being evaluated.</param>
    public void EnsureStability(string activityName)
    {
        // 50% threshold for high-frequency failure simulation
        if (_random.NextDouble() < 0.5)
        {
            logger.LogInformation("🔥 CHAOS: Forcing failure in {Activity}", activityName);
            throw new ChaosException($"Simulated failure in {activityName}");
        }

        logger.LogInformation("✅ STABILITY: {Activity} passed chaos check.", activityName);
    }
}
