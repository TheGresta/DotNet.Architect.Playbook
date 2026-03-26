using MassTransit;

using Microsoft.EntityFrameworkCore;

using Playbook.Messaging.MassTransit.Domain;

namespace Playbook.Messaging.MassTransit.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This maps the Saga State Machine to the DB
        modelBuilder.Entity<WorkflowState>().HasKey(x => x.CorrelationId);

        // These helper methods add the Transactional Outbox tables
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
