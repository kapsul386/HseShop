using System.Text;
using System.Text.Json;
using Contracts;
using NotificationsService.Ws;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NotificationsService.Messaging;

public sealed class PaymentsEventsConsumer : BackgroundService
{
    private readonly IRabbitConnection _rabbit;
    private readonly WsHub _hub;

    public PaymentsEventsConsumer(IRabbitConnection rabbit, WsHub hub)
    {
        _rabbit = rabbit;
        _hub = hub;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ch = _rabbit.CreateChannel();

        var q = ch.QueueDeclare(
            queue: "notifications.payments-events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        ch.QueueBind(q.QueueName, Routing.Exchange, Routing.PaymentsSucceeded);
        ch.QueueBind(q.QueueName, Routing.Exchange, Routing.PaymentsFailed);

        ch.BasicQos(0, 50, false);

        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                if (ea.RoutingKey == Routing.PaymentsSucceeded)
                {
                    var evt = JsonSerializer.Deserialize<PaymentSucceededV1>(json);
                    if (evt is null) throw new InvalidOperationException("Bad PaymentSucceededV1");
                    await _hub.NotifyAsync(evt.OrderId, "FINISHED", null, stoppingToken);
                }
                else if (ea.RoutingKey == Routing.PaymentsFailed)
                {
                    var evt = JsonSerializer.Deserialize<PaymentFailedV1>(json);
                    if (evt is null) throw new InvalidOperationException("Bad PaymentFailedV1");
                    await _hub.NotifyAsync(evt.OrderId, "CANCELLED", evt.Reason, stoppingToken);
                }

                ch.BasicAck(ea.DeliveryTag, false);
            }
            catch
            {
                ch.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        ch.BasicConsume(q.QueueName, autoAck: false, consumer: consumer);
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
