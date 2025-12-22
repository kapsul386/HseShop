using System.Text.Json;
using Contracts;
using Microsoft.EntityFrameworkCore;
using OrdersService.Application.Orders.Dtos;
using OrdersService.Infrastructure.Http;
using OrdersService.Persistence;

namespace OrdersService.Application.Orders;

public sealed class OrdersService : IOrdersService
{
    private readonly OrdersDbContext _db;
    private readonly IUserContext _user;

    public OrdersService(OrdersDbContext db, IUserContext user)
    {
        _db = db;
        _user = user;
    }

    public async Task<CreateOrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Amount must be positive.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = _user.UserId,
            Amount = request.Amount,
            Status = OrderStatus.NEW,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Orders.Add(order);

        var evt = new OrderCreatedV1(order.Id, order.UserId, order.Amount);
        var payload = JsonSerializer.Serialize(evt);

        _db.Outbox.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(OrderCreatedV1),
            RoutingKey = Routing.OrdersCreated,
            PayloadJson = payload,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new CreateOrderResponse(order.Id);
    }

    public async Task<List<OrderView>> ListAsync(CancellationToken ct)
    {
        var userId = _user.UserId;

        return await _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderView(
                o.Id,
                o.Amount,
                o.Status.ToString(),
                o.CreatedAtUtc,
                o.CancelReason))
            .ToListAsync(ct);
    }

    public async Task<OrderView?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var userId = _user.UserId;

        var o = await _db.Orders
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

        return o is null
            ? null
            : new OrderView(o.Id, o.Amount, o.Status.ToString(), o.CreatedAtUtc, o.CancelReason);
    }
}
