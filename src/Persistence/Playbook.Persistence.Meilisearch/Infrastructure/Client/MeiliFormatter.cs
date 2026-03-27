using System.Globalization;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

/// <summary>
/// A high-performance static utility designed to bridge the gap between .NET primitive types 
/// and the domain-specific language (DSL) required by Meilisearch for filtering and sorting.
/// This class ensures that data types are serialized into a format that the search engine 
/// can interpret correctly, specifically handling quoting, escaping, and ISO date formatting.
/// </summary>
public static class MeiliFormatter
{
    /// <summary>
    /// Converts a .NET object into its equivalent Meilisearch filter string representation.
    /// Supports strings, booleans, various date formats, and formattable primitives.
    /// </summary>
    /// <param name="value">The object to be formatted. Can be null.</param>
    /// <returns>A string representation compatible with Meilisearch filter syntax.</returns>
    /// <remarks>
    /// Dates are converted to ISO 8601 strings, and strings are automatically escaped 
    /// to prevent injection or syntax errors in the filter string.
    /// </remarks>
    public static string Format(object? value) => value switch
    {
        // Explicitly handle null values for the Meilisearch DSL.
        null => "null",

        // Escapes double quotes within strings and wraps the entire value in quotes.
        string s => $"\"{s.Replace("\"", "\\\"")}\"",

        // Meilisearch expects lowercase boolean literals.
        bool b => b ? "true" : "false",

        // Standardizing date formats to UTC ISO 8601 as required by Meilisearch for correct range filtering.
        DateTime dt => $"\"{dt.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}\"",
        DateTimeOffset dto => $"\"{dto.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}\"",

        Enum e => $"\"{e.ToString().Replace("\"", "\\\"")}\"",

        // Handles numeric types (int, double, decimal) using InvariantCulture to ensure 
        // the decimal separator is always a dot (.) regardless of the host system locale.
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),

        // Default fallback for any other types.
        _ => $"\"{value.ToString()?.Replace("\"", "\\\"")}\""
    };
}
