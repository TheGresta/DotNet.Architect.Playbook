using System.ComponentModel.DataAnnotations;

namespace Playbook.Persistence.MongoDB.Infrastructure.Contexts;

public class MongoDbOptions
{
    [Required] public string ConnectionString { get; set; } = string.Empty;
    [Required] public string DatabaseName { get; set; } = string.Empty;
}
