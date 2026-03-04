namespace Playbook.Persistence.ElasticSearch.Application.Models;

public abstract class BaseDocument
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
}
