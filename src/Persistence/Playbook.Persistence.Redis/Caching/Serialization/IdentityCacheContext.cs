using System.Text.Json.Serialization;

using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis.Caching.Serialization;

/// <summary>
/// Provides a source-generated <see cref="JsonSerializerContext"/> for types within the Identity module.
/// </summary>
/// <remarks>
/// This context enables high-performance, reflection-free serialization for <see cref="UserDto"/> 
/// and its associated collections and envelopes.
/// </remarks>
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(List<UserDto>))]
[JsonSerializable(typeof(CacheEnvelope<UserDto>))]
[JsonSerializable(typeof(CacheEnvelope<List<UserDto>>))]
internal partial class IdentityCacheContext : JsonSerializerContext;
