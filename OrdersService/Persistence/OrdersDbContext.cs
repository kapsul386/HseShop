using Microsoft.EntityFrameworkCore;

namespace OrdersService.Persistence;

public sealed class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Order>()
            .HasIndex(x => new { x.UserId, x.CreatedAtUtc });

        modelBuilder.Entity<OutboxMessage>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<OutboxMessage>()
            .HasIndex(x => x.OccurredAtUtc);

        base.OnModelCreating(modelBuilder);
    }
}

public enum OrderStatus
{
    NEW = 0,
    FINISHED = 1,
    CANCELLED = 2
}

public sealed class Order
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public decimal Amount { get; set; }
    public OrderStatus Status { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
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