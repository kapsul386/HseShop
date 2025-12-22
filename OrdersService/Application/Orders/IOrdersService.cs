using OrdersService.Application.Orders.Dtos;

namespace OrdersService.Application.Orders;

public interface IOrdersService
{
    Task<CreateOrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken ct);
    Task<List<OrderView>> ListAsync(CancellationToken ct);
    Task<OrderView?> GetByIdAsync(Guid id, CancellationToken ct);
}