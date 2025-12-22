using RabbitMQ.Client;

namespace PaymentsService.Infrastructure.Messaging;

public interface IRabbitConnection : IDisposable
{
    IModel CreateChannel();
}