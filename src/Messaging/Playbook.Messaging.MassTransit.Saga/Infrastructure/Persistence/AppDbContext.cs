using MassTransit;

using Microsoft.EntityFrameworkCore;

using Playbook.Messaging.MassTransit.Saga.Domain;

namespace Playbook.Messaging.MassTransit.Saga.Infrastructure.Persistence;

/// <summary>
/// The primary Entity Framework Core database context for the application.
/// Manages persistence for the Saga state machine instances and provides storage for the 
/// MassTransit Transactional Outbox and Inbox patterns to ensure "exactly-once" message processing.
/// </summary>
/// <param name="options">The options to be used by the <see cref="AppDbContext"/>.</param>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the database set for persisting <see cref="WorkflowState"/> instances.
    /// This table tracks the current progress and data of active sagas.
    /// </summary>
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();

    /// <summary>
    /// Configures the model mapping for the database context.
    /// Includes configuration for Saga primary keys and the necessary schema for 
    /// MassTransit's outbox components.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Explicitly define the CorrelationId as the primary key for the Saga State Machine.
        // This is essential for the repository to look up existing saga instances during message correlation.
        modelBuilder.Entity<WorkflowState>().HasKey(x => x.CorrelationId);

        // Configures the InboxState entity which tracks received messages to prevent duplicate processing.
        modelBuilder.AddInboxStateEntity();

        // Configures the OutboxMessage entity where outgoing messages are persisted before being published to the broker.
        modelBuilder.AddOutboxMessageEntity();

        // Configures the OutboxState entity which manages the delivery status of messages stored in the outbox.
        modelBuilder.AddOutboxStateEntity();
    }
}
