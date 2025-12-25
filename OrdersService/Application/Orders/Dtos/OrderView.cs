namespace OrdersService.Application.Orders.Dtos;

/// <summary>
/// Представление заказа для чтения (query-модель).
/// </summary>
public sealed record OrderView(
    Guid Id,
    decimal Amount,
    string Status,
    DateTime CreatedAtUtc,
    string? CancelReason);