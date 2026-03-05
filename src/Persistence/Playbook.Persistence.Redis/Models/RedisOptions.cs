using System.ComponentModel.DataAnnotations;

namespace Playbook.Persistence.Redis.Models;

public class RedisOptions
{
    public const string SectionName = "Redis";

    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;

    public string InstanceName { get; set; } = string.Empty;

    public TimeSpan? DefaultExpiration { get; set; }

    public bool AbortOnConnectFail { get; set; } = false;

    [Range(100, 30000)] // Ensure timeout is within sane bounds (ms)
    public int SyncTimeout { get; set; } = 5000;
}
