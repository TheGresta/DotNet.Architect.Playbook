namespace Playbook.Architecture.CQRS.Domain.Constants;

public static class LocalizationPrefixes
{
    /// <summary>Prefix for informational titles or headers.</summary>
    public const string Info = "INF_";
    /// <summary>Prefix for detailed message templates.</summary>
    public const string Detail = "DET_";
    /// <summary>Prefix for generic nouns and entity names.</summary>
    public const string Resource = "RES_";
    /// <summary>Prefix for validation-specific rule keys.</summary>
    public const string Validation = "VAL_";
    /// <summary>Prefix for domain business rule keys.</summary>
    public const string Rule = "RULE_";
}
