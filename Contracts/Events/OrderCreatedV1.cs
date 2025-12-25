namespace Contracts;

/// <summary>
/// Событие, публикуемое OrdersService после создания заказа.
/// Используется для асинхронного запуска процесса оплаты.
/// </summary>
public sealed record OrderCreatedV1(
    Guid OrderId,
    string UserId,
    decimal Amount);