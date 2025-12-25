using Microsoft.AspNetCore.Mvc;
using OrdersService.Application.Orders;
using OrdersService.Application.Orders.Dtos;

namespace OrdersService.Controllers;

/// <summary>
/// HTTP API для работы с заказами.
/// Используется через ApiGateway (префикс /orders).
/// </summary>
[ApiController]
[Route("")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrdersService _orders;

    public OrdersController(IOrdersService orders) => _orders = orders;

    /// <summary>
    /// Создаёт новый заказ и асинхронно инициирует процесс оплаты.
    /// </summary>
    [HttpPost]
    [HttpPost("orders")]
    public async Task<ActionResult<CreateOrderResponse>> Create(
        [FromBody] CreateOrderRequest req,
        CancellationToken ct)
        => Ok(await _orders.CreateAsync(req, ct));

    /// <summary>
    /// Возвращает список заказов текущего пользователя.
    /// </summary>
    [HttpGet]
    [HttpGet("orders")]
    public async Task<ActionResult<List<OrderView>>> List(CancellationToken ct)
        => Ok(await _orders.ListAsync(ct));

    /// <summary>
    /// Возвращает информацию о заказе по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<OrderView>> Get(Guid id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        return order is null ? NotFound("Order not found") : Ok(order);
    }
}