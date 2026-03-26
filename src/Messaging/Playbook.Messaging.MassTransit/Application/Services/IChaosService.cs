namespace Playbook.Messaging.MassTransit.Application.Services;

/// <summary>
/// Defines a service used to simulate transient or random failures within the system.
/// Primarily used for testing saga resiliency and compensation logic under unstable conditions.
/// </summary>
public interface IChaosService
{
    /// <summary>
    /// Evaluates a randomized condition and potentially throws an exception to simulate a service failure.
    /// </summary>
    /// <param name="stepName">The name of the workflow step where the chaos is being injected for logging purposes.</param>
    /// <exception cref="Exception">Thrown when the "unlucky" condition is met.</exception>
    void ThrowIfUnlucky(string stepName);
}

/// <summary>
/// A concrete implementation of <see cref="IChaosService"/> that uses a pseudo-random number generator
/// to trigger failures with a predefined probability.
/// </summary>
public class ChaosService : IChaosService
{
    /// <summary>
    /// Executes a probability check (50% failure rate) and throws an exception if the check fails.
    /// </summary>
    /// <param name="stepName">The name of the current execution context for the failure message.</param>
    /// <exception cref="Exception">Injected failure representing a simulated system error.</exception>
    public void ThrowIfUnlucky(string stepName)
    {
        // 50% chance of failure: generates a value between 0 and 99.
        // If the value is less than 50, a synthetic exception is triggered.
        if (Random.Shared.Next(0, 100) < 50)
        {
            throw new Exception($"[CHAOS] Random failure injected at {stepName}!");
        }
    }
}
