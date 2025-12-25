namespace OrdersService.Application.Orders.Dtos;

/// <summary>
/// Запрос на создание заказа.
/// </summary>
public sealed record CreateOrderRequest(decimal Amount);