namespace Contracts;

/// <summary>
/// Константы маршрутизации сообщений RabbitMQ.
/// Используются всеми сервисами для согласованного обмена событиями.
/// </summary>
public static class Routing
{
    /// <summary>
    /// Основной exchange системы.
    /// </summary>
    public const string Exchange = "hse.shop";

    /// <summary>
    /// Событие создания заказа.
    /// </summary>
    public const string OrdersCreated = "orders.created";

    /// <summary>
    /// Событие успешной оплаты заказа.
    /// </summary>
    public const string PaymentsSucceeded = "payments.succeeded";

    /// <summary>
    /// Событие неуспешной оплаты заказа.
    /// </summary>
    public const string PaymentsFailed = "payments.failed";
}