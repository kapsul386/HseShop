using Microsoft.EntityFrameworkCore;
using OrdersService.Persistence;

namespace OrdersService.Infrastructure.Messaging.Consumers;

public sealed class PaymentResultHandler
{
    private readonly OrdersDbContext _db;

    public PaymentResultHandler(OrdersDbContext db) => _db = db;

    public Task ApplySucceededAsync(Guid orderId, CancellationToken ct)
    {
        // Идемпотентно: обновляем только если заказ ещё NEW
        return _db.Orders
            .Where(o => o.Id == orderId && o.Status == OrderStatus.NEW)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OrderStatus.FINISHED)
                .SetProperty(o => o.CancelReason, (string?)null), ct);
    }

    public Task ApplyFailedAsync(Guid orderId, string reason, CancellationToken ct)
    {
        return _db.Orders
            .Where(o => o.Id == orderId && o.Status == OrderStatus.NEW)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, OrderStatus.CANCELLED)
                .SetProperty(o => o.CancelReason, reason), ct);
    }
}