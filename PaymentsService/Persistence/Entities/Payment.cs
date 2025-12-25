namespace PaymentsService.Persistence;

/// <summary>
/// Факт успешной оплаты заказа.
/// Уникальность OrderId гарантирует exactly-once списание.
/// </summary>
public sealed class Payment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = default!;
    public decimal Amount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}