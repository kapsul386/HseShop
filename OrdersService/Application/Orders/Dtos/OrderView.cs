namespace OrdersService.Application.Orders.Dtos;

public sealed record OrderView(
    Guid Id,
    decimal Amount,
    string Status,
    DateTime CreatedAtUtc,
    string? CancelReason);