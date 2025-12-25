namespace Contracts;

/// <summary>
/// Событие неуспешной оплаты заказа.
/// Содержит причину отказа (например, недостаточно средств).
/// </summary>
public sealed record PaymentFailedV1(
    Guid OrderId,
    string UserId,
    decimal Amount,
    string Reason);