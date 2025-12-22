using System.Text;
using System.Text.Json;
using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrdersService.Infrastructure.Messaging.Consumers;

public sealed class PaymentsEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitConnection _rabbit;

    public PaymentsEventsConsumer(IServiceScopeFactory scopeFactory, IRabbitConnection rabbit)
    {
        _scopeFactory = scopeFactory;
        _rabbit = rabbit;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ch = _rabbit.CreateChannel();

        var q = ch.QueueDeclare(
            queue: "orders.payment-results",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        ch.QueueBind(q.QueueName, Routing.Exchange, Routing.PaymentsSucceeded);
        ch.QueueBind(q.QueueName, Routing.Exchange, Routing.PaymentsFailed);

        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<PaymentResultHandler>();

                if (ea.RoutingKey == Routing.PaymentsSucceeded)
                {
                    var evt = JsonSerializer.Deserialize<PaymentSucceededV1>(json);
                    if (evt is null) throw new InvalidOperationException("Invalid PaymentSucceededV1 payload.");

                    await handler.ApplySucceededAsync(evt.OrderId, stoppingToken);
                }
                else if (ea.RoutingKey == Routing.PaymentsFailed)
                {
                    var evt = JsonSerializer.Deserialize<PaymentFailedV1>(json);
                    if (evt is null) throw new InvalidOperationException("Invalid PaymentFailedV1 payload.");

                    await handler.ApplyFailedAsync(evt.OrderId, evt.Reason, stoppingToken);
                }

                ch.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch
            {
                ch.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        ch.BasicConsume(queue: q.QueueName, autoAck: false, consumer: consumer);

        // держим сервис живым, пока не отменят
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
