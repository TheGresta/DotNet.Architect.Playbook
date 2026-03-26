namespace Playbook.Messaging.MassTransit.Application.Services;

public interface IChaosService
{
    void ThrowIfUnlucky(string stepName);
}

public class ChaosService : IChaosService
{
    public void ThrowIfUnlucky(string stepName)
    {
        // 50% chance of failure
        if (Random.Shared.Next(0, 100) < 50)
        {
            throw new Exception($"[CHAOS] Random failure injected at {stepName}!");
        }
    }
}
