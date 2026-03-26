namespace Playbook.Messaging.MassTransit.Courier.Messaging;

public interface IChaosProvider
{
    void EnsureStability(string activityName);
}

public sealed class ChaosException(string message) : Exception(message);

public sealed class ChaosProvider(ILogger<ChaosProvider> logger) : IChaosProvider
{
    private readonly Random _random = new();

    public void EnsureStability(string activityName)
    {
        // 50% threshold
        if (_random.NextDouble() < 0.5)
        {
            logger.LogInformation("🔥 CHAOS: Forcing failure in {Activity}", activityName);
            throw new ChaosException($"Simulated failure in {activityName}");
        }

        logger.LogInformation("✅ STABILITY: {Activity} passed chaos check.", activityName);
    }
}
