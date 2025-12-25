using OrdersService.Application.Orders.Dtos;

namespace OrdersService.Application.Orders;

/// <summary>
/// Контракт сервиса работы с заказами.
/// </summary>
public interface IOrdersService
{
    /// <summary>
    /// Создаёт заказ и инициирует асинхронный процесс оплаты.
    /// </summary>
    Task<CreateOrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct);

    /// <summary>
    /// Возвращает список заказов текущего пользователя.
    /// </summary>
    Task<List<OrderView>> ListAsync(CancellationToken ct);

    /// <summary>
    /// Возвращает заказ по идентификатору или null, если он не найден.
    /// </summary>
    Task<OrderView?> GetByIdAsync(Guid id, CancellationToken ct);
}