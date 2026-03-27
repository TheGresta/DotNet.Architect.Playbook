using System.Globalization;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

/// <summary>
/// Static utility for converting .NET types into Meilisearch-compatible filter strings.
/// </summary>
public static class MeiliFormatter
{
    public static string Format(object? value) => value switch
    {
        null => "null",
        string s => $"\"{s.Replace("\"", "\\\"")}\"",
        bool b => b ? "true" : "false",
        DateTime dt => $"\"{dt:yyyy-MM-ddTHH:mm:ssZ}\"",
        DateTimeOffset dto => $"\"{dto:yyyy-MM-ddTHH:mm:ssZ}\"",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()?.ToLowerInvariant() ?? "null"
    };
}
