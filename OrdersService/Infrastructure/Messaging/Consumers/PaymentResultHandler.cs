using Microsoft.EntityFrameworkCore;
using OrdersService.Persistence;

namespace OrdersService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Применяет результаты оплаты к заказам.
/// Операции выполняются идемпотентно.
/// </summary>
public sealed class PaymentResultHandler
{
    private readonly OrdersDbContext _db;

    public PaymentResultHandler(OrdersDbContext db) => _db = db;

    /// <summary>
    /// Помечает заказ как FINISHED, если он ещё находится в статусе NEW.
    /// </summary>
    public Task ApplySucceededAsync(Guid orderId, CancellationToken ct)
        => _db.Orders
            .Where(o => o.Id == orderId && o.Status == OrderStatus.NEW)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OrderStatus.FINISHED)
                .SetProperty(o => o.CancelReason, (string?)null), ct);

    /// <summary>
    /// Помечает заказ как CANCELLED с указанием причины.
    /// </summary>
    public Task ApplyFailedAsync(Guid orderId, string reason, CancellationToken ct)
        => _db.Orders
            .Where(o => o.Id == orderId && o.Status == OrderStatus.NEW)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OrderStatus.CANCELLED)
                .SetProperty(o => o.CancelReason, reason), ct);
}