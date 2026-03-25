using System.Text.Json.Serialization;

using Playbook.Messaging.RabbitMQ.Models;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal.Serialization;

/// <summary>
/// Provides a high-performance, source-generated <see cref="JsonSerializerContext"/> for the messaging subsystem.
/// By utilizing .NET 10 source generation, this context eliminates the need for runtime reflection during 
/// serialization and deserialization, significantly reducing CPU overhead and memory allocations.
/// </summary>
/// <remarks>
/// This partial class is augmented by the C# compiler to generate optimized metadata for the 
/// specified types. It is configured with strict performance-oriented options such as 
/// disabled indentation and camelCase naming policies.
/// </remarks>
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(OrderCreated))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class MessagingJsonContext : JsonSerializerContext;
