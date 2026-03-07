using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

using Playbook.Persistence.EntityFramework.Domain.Base;
using Playbook.Persistence.EntityFramework.Persistence.Options;

namespace Playbook.Persistence.EntityFramework.Persistence.Interceptors;

/// <summary>
/// Intercepts database save operations to automatically populate audit metadata for 
/// entities deriving from <see cref="AuditableEntity"/>.
/// </summary>
/// <remarks>
/// This interceptor hooks into the <see cref="DbContext"/> lifecycle to manage 
/// timestamps and user attribution without requiring manual assignment in the application layer.
/// </remarks>
public class AuditableEntityInterceptor(IOptions<DbOptions> dbOptions) : SaveChangesInterceptor
{
    private readonly DbOptions _dbOptions = dbOptions.Value;

    /// <summary>
    /// Synchronously intercepts the saving process to update audit information.
    /// </summary>
    /// <inheritdoc/>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Asynchronously intercepts the saving process to update audit information.
    /// </summary>
    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Scans the change tracker for <see cref="AuditableEntity"/> instances and applies 
    /// creation or modification metadata based on the <see cref="EntityState"/>.
    /// </summary>
    /// <param name="context">The current <see cref="DbContext"/> instance.</param>
    /// <remarks>
    /// <para>
    /// <b>Logic:</b>
    /// 1. <b>Capture Clock:</b> Uses a single <see cref="DateTime.UtcNow"/> for all entries in the batch to ensure consistency.<br/>
    /// 2. <b>Handling New Entities:</b> If an entity is <see cref="EntityState.Added"/>, sets <c>CreatedAt</c>, <c>CreatedBy</c>, and ensures <c>IsActive</c> is initialized.<br/>
    /// 3. <b>Handling Modified Entities:</b> If an entity is <see cref="EntityState.Modified"/>, sets <c>UpdatedAt</c> and <c>UpdatedBy</c>.<br/>
    /// 4. <b>Immutability Protection:</b> Explicitly marks <c>CreatedAt</c> and <c>CreatedBy</c> as unmodified during updates to prevent tampering or accidental overrides.
    /// </para>
    /// </remarks>
    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries<AuditableEntity>();
        var transactionDate = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = transactionDate;
                entry.Entity.CreatedBy = _dbOptions.ApplicationName;
                entry.Entity.IsActive = true;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = transactionDate;
                entry.Entity.UpdatedBy = _dbOptions.ApplicationName;

                // Technical Safeguard: Prevents the database from updating these values
                // even if they were changed in the tracked object instance.
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Property(x => x.CreatedBy).IsModified = false;
            }
        }
    }
}
