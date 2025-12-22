using RabbitMQ.Client;

namespace OrdersService.Infrastructure.Messaging;

public interface IRabbitConnection : IDisposable
{
    IModel CreateChannel();
}