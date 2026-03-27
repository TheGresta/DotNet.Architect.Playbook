using System.Text.Json.Serialization;

using Playbook.Persistence.Meilisearch.Core.Models;

namespace Playbook.Persistence.Meilisearch.Core.Serialization;

/// <summary>
/// .NET 10 Source Generator for JSON. 
/// Eliminates reflection at runtime, reducing memory overhead and latency.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(CarDocument))]
[JsonSerializable(typeof(IEnumerable<CarDocument>))]
internal partial class MeiliJsonContext : JsonSerializerContext
{
}
