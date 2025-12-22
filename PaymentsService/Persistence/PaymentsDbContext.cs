using Microsoft.EntityFrameworkCore;

namespace PaymentsService.Persistence;

public sealed class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>()
            .HasKey(x => x.UserId);

        modelBuilder.Entity<Payment>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Payment>()
            .HasIndex(x => x.OrderId)
            .IsUnique();

        modelBuilder.Entity<InboxMessage>()
            .HasKey(x => x.MessageId);

        modelBuilder.Entity<OutboxMessage>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(x => x.OccurredAtUtc);

        base.OnModelCreating(modelBuilder);
    }
}