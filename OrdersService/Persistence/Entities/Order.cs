namespace OrdersService.Persistence;

public sealed class Order
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public decimal Amount { get; set; }
    public OrderStatus Status { get; set; }
    public string? CancelReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}