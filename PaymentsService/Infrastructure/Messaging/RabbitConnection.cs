using Contracts;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PaymentsService.Infrastructure.Messaging;

/// <summary>
/// Реализация подключения к RabbitMQ с объявлением exchange.
/// </summary>
public sealed class RabbitConnection : IRabbitConnection
{
    private readonly IConnection _conn;

    public RabbitConnection(IOptions<RabbitOptions> opt)
    {
        var o = opt.Value;

        var factory = new ConnectionFactory
        {
            HostName = o.Host,
            Port = o.Port,
            UserName = o.User,
            Password = o.Pass,
            DispatchConsumersAsync = true
        };

        _conn = factory.CreateConnection();
    }

    public IModel CreateChannel()
    {
        var ch = _conn.CreateModel();
        ch.ExchangeDeclare(
            exchange: Routing.Exchange,
            type: ExchangeType.Topic,
            durable: true);
        return ch;
    }

    public void Dispose()
    {
        try { _conn.Close(); } catch { /* ignore */ }
        _conn.Dispose();
    }
}