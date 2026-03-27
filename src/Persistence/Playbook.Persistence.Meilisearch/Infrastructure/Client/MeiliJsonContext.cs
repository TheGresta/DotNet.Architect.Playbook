using System.Text.Json.Serialization;

using Playbook.Persistence.Meilisearch.Core.Models;

namespace Playbook.Persistence.Meilisearch.Core.Serialization;

/// <summary>
/// A high-performance, ahead-of-time (AOT) compiled JSON serialization context.
/// By leveraging .NET 10 Source Generators, this class eliminates the need for 
/// runtime reflection during the serialization and deserialization of search documents.
/// </summary>
/// <remarks>
/// This implementation significantly reduces memory allocation and cold-start latency (JIT), 
/// making it ideal for high-throughput search operations and environments with strict 
/// performance constraints like serverless or resource-constrained containers.
/// </remarks>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(CarDocument))]
[JsonSerializable(typeof(IEnumerable<CarDocument>))]
internal partial class MeiliJsonContext : JsonSerializerContext
{
    // The source generator automatically implements the partial members of this class 
    // at compile-time based on the [JsonSerializable] attributes provided above.
}
