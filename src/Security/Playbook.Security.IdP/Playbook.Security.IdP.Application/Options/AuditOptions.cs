namespace Playbook.Security.IdP.Application.Options;

public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    /// <summary>Service name embedded in every audit record (e.g. "IdP").</summary>
    public string ServiceName { get; init; } = "IdP";
    public string EnvironmentName { get; init; } = null!;
}
