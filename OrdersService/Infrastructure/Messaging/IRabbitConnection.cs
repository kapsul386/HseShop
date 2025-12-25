using RabbitMQ.Client;

namespace OrdersService.Infrastructure.Messaging;

/// <summary>
/// Абстракция подключения к RabbitMQ.
/// </summary>
public interface IRabbitConnection : IDisposable
{
    /// <summary>
    /// Создаёт канал (model) для работы с брокером сообщений.
    /// </summary>
    IModel CreateChannel();
}