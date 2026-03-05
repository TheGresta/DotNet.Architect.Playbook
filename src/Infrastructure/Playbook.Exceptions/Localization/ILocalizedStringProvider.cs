namespace Playbook.Exceptions.Localization;

public interface ILocalizedStringProvider
{
    string Get(string key, params object[] args);
}
