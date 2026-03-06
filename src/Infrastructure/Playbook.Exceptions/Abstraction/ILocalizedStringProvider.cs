namespace Playbook.Exceptions.Abstraction;

public interface ILocalizedStringProvider
{
    string Get(string key, params object[] args);
}
