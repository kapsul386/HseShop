using RabbitMQ.Client;

namespace PaymentsService.Infrastructure.Messaging;

/// <summary>
/// Абстракция подключения к RabbitMQ.
/// </summary>
public interface IRabbitConnection : IDisposable
{
    /// <summary>
    /// Создаёт канал для работы с брокером сообщений.
    /// </summary>
    IModel CreateChannel();
}