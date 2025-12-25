namespace OrdersService.Application.Orders.Dtos;

/// <summary>
/// Результат создания заказа.
/// </summary>
public sealed record CreateOrderResponse(Guid OrderId);