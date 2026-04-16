using System.Linq.Expressions;

namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// Represents a single eager-loading step, optionally chained with nested includes.
/// </summary>
/// <typeparam name="TEntity">The root entity type.</typeparam>
public sealed class IncludePath<TEntity>
{
    private IncludePath() { }

    /// <summary>Gets the root navigation property expression.</summary>
    public LambdaExpression NavigationExpression { get; private init; } = null!;

    /// <summary>Gets chained nested include expressions (ThenInclude), if any.</summary>
    public IReadOnlyList<LambdaExpression> ThenIncludes { get; private init; } = [];

    internal static IncludePath<TEntity> Create(
        LambdaExpression navigation,
        IReadOnlyList<LambdaExpression> thenIncludes)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(thenIncludes);

        return new()
        {
            NavigationExpression = navigation,
            ThenIncludes = [.. thenIncludes]
        };
    }
}
