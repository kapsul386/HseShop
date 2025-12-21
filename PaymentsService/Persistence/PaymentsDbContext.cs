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

        // гарант “списание не дважды” на один заказ
        modelBuilder.Entity<Payment>()
            .HasIndex(x => x.OrderId)
            .IsUnique();

        // Inbox дедуп по messageId (будем брать из RabbitMQ MessageId)
        modelBuilder.Entity<InboxMessage>()
            .HasKey(x => x.MessageId);

        modelBuilder.Entity<OutboxMessage>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(x => x.OccurredAtUtc);

        base.OnModelCreating(modelBuilder);
    }
}

public sealed class Account
{
    public string UserId { get; set; } = default!;
    public decimal Balance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class InboxMessage
{
    public Guid MessageId { get; set; }
    public string Type { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime ReceivedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }
    public string? LastError { get; set; }
    public int ProcessAttempts { get; set; }
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime OccurredAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }
    public int PublishAttempts { get; set; }
    public string? LastError { get; set; }
}
