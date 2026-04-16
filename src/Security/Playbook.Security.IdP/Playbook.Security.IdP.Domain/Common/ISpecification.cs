using System.Linq.Expressions;

namespace Playbook.Security.IdP.Domain.Common;

/// <summary>
/// A "Gold Standard" Specification contract that bridges 
/// Domain Logic and Infrastructure Persistence.
/// </summary>
public interface ISpecification<T>
{
    /// <summary>
    /// Evaluates if a specific entity instance satisfies the business rule in-memory.
    /// Used for domain validation.
    /// </summary>
    bool IsSatisfiedBy(T entity);

    /// <summary>
    /// Returns an expression tree that can be translated by EF Core/LINQ into SQL.
    /// Used for high-performance database filtering.
    /// </summary>
    Expression<Func<T, bool>> ToExpression();
}
