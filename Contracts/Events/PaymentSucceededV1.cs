namespace Contracts;

/// <summary>
/// Событие успешной оплаты заказа.
/// Публикуется PaymentsService и обрабатывается OrdersService.
/// </summary>
public sealed record PaymentSucceededV1(
    Guid OrderId,
    string UserId,
    decimal Amount,
    Guid PaymentId);