using Microsoft.EntityFrameworkCore;

namespace OrdersService.Persistence;

/// <summary>
/// Контекст базы данных OrdersService.
/// </summary>
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