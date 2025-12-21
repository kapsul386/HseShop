using System.Text.Json;
using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersService.Persistence;

namespace OrdersService.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly OrdersDbContext _db;

    public OrdersController(OrdersDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        if (request.Amount <= 0)
            return BadRequest("Amount must be positive.");

        // NEW заказ + outbox (в одной транзакции) — это “Transactional Outbox” из требований :contentReference[oaicite:2]{index=2}
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
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

        return Ok(new CreateOrderResponse(order.Id));
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderView>>> List(CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var items = await _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderView(o.Id, o.Amount, o.Status.ToString(), o.CreatedAtUtc, o.CancelReason))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderView>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var userId = GetUserIdOrThrow();

        var o = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (o is null) return NotFound();

        return Ok(new OrderView(o.Id, o.Amount, o.Status.ToString(), o.CreatedAtUtc, o.CancelReason));
    }

    private string GetUserIdOrThrow()
    {
        if (Request.Headers.TryGetValue("X-User-Id", out var v) && !string.IsNullOrWhiteSpace(v))
            return v.ToString();

        throw new InvalidOperationException("Missing header X-User-Id");
    }
}

public record CreateOrderRequest(decimal Amount);

public record CreateOrderResponse(Guid OrderId);

public record OrderView(Guid Id, decimal Amount, string Status, DateTime CreatedAtUtc, string? CancelReason);
