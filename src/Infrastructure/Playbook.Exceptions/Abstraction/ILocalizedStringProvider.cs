namespace Playbook.Exceptions.Abstraction;

/// <summary>
/// Defines a provider for retrieving localized strings, facilitating multi-language error messaging.
/// </summary>
public interface ILocalizedStringProvider
{
    /// <summary>
    /// Retrieves a localized string based on the provided key and format arguments.
    /// </summary>
    /// <param name="key">The unique key representing the localized resource.</param>
    /// <param name="args">Format arguments to inject into the localized string template.</param>
    /// <returns>The localized and formatted string.</returns>
    string Get(string key, params object[] args);
}
