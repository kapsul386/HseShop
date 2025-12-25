namespace OrdersService.Persistence;

/// <summary>
/// Доменная сущность заказа.
/// </summary>
public sealed class Order
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public decimal Amount { get; set; }

    /// <summary>
    /// Текущий статус заказа.
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Причина отмены заказа (заполняется при статусе CANCELLED).
    /// </summary>
    public string? CancelReason { get; set; }

    /// <summary>
    /// Дата и время создания заказа (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}