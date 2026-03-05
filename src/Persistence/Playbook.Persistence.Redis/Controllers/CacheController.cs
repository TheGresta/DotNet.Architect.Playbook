using Microsoft.AspNetCore.Mvc;
using Playbook.Persistence.Redis.Interfaces;

namespace Playbook.Persistence.Redis.Controllers;

[ApiController]
[Route("api/cache")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cache;

    public CacheController(ICacheService cache)
    {
        _cache = cache;
    }

    // POST api/cache/invalidate-prefix/{prefix}
    [HttpPost("invalidate-prefix/{prefix}")]
    public async Task<IActionResult> InvalidatePrefix(string prefix, CancellationToken ct)
    {
        await _cache.InvalidatePrefixAsync(prefix, ct);
        return Ok($"Prefix '{prefix}' invalidated.");
    }

    // DELETE api/cache/{key}
    [HttpDelete("{key}")]
    public async Task<IActionResult> Remove(string key, CancellationToken ct)
    {
        await _cache.RemoveAsync(key, ct);
        return Ok($"Key '{key}' removed.");
    }

    // POST api/cache/remove-by-prefix/{prefix}
    [HttpPost("remove-by-prefix/{prefix}")]
    public async Task<IActionResult> RemoveByPrefix(string prefix, CancellationToken ct)
    {
        await _cache.RemoveByPrefixAsync(prefix, ct);
        return Ok($"Physical keys with prefix '{prefix}' removed from Redis.");
    }
}