using Microsoft.AspNetCore.Mvc;
using OrdersService.Application.Orders;
using OrdersService.Application.Orders.Dtos;

namespace OrdersService.Controllers;

[ApiController]
[Route("")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrdersService _orders;

    public OrdersController(IOrdersService orders) => _orders = orders;

    // Создание заказа: принимает и POST /, и POST /orders
    [HttpPost]
    [HttpPost("orders")]
    public async Task<ActionResult<CreateOrderResponse>> Create([FromBody] CreateOrderRequest req, CancellationToken ct)
        => Ok(await _orders.CreateAsync(req, ct));

    // Список заказов: GET / и GET /orders
    [HttpGet]
    [HttpGet("orders")]
    public async Task<ActionResult<List<OrderView>>> List(CancellationToken ct)
        => Ok(await _orders.ListAsync(ct));

    // Статус заказа: GET /{id} и GET /orders/{id}
    [HttpGet("{id:guid}")]
    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<OrderView>> Get(Guid id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        return order is null ? NotFound("Order not found") : Ok(order);
    }
}