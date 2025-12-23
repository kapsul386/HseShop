using RabbitMQ.Client;

namespace NotificationsService.Messaging;

public interface IRabbitConnection : IDisposable
{
    IModel CreateChannel();
}
