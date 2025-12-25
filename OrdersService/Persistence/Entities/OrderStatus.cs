namespace OrdersService.Persistence;

/// <summary>
/// Возможные статусы заказа.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Заказ создан и ожидает результата оплаты.
    /// </summary>
    NEW = 0,

    /// <summary>
    /// Оплата прошла успешно.
    /// </summary>
    FINISHED = 1,

    /// <summary>
    /// Оплата не удалась (например, недостаточно средств).
    /// </summary>
    CANCELLED = 2
}