using System.Text;
using System.Text.Json;
using Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentsService.Infrastructure.Messaging.Consumers;

public sealed class OrdersCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitConnection _rabbit;

    public OrdersCreatedConsumer(IServiceScopeFactory scopeFactory, IRabbitConnection rabbit)
    {
        _scopeFactory = scopeFactory;
        _rabbit = rabbit;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ch = _rabbit.CreateChannel();

        var q = ch.QueueDeclare(
            queue: "payments.orders-created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        ch.QueueBind(q.QueueName, Routing.Exchange, Routing.OrdersCreated);

        ch.BasicQos(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(ch);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var messageId = TryParseGuid(ea.BasicProperties?.MessageId) ?? Guid.NewGuid();
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                var evt = JsonSerializer.Deserialize<OrderCreatedV1>(json);
                if (evt is null)
                    throw new InvalidOperationException("Invalid OrderCreatedV1 payload.");

                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OrderPaymentProcessor>();

                await processor.ProcessAsync(messageId, evt, json, stoppingToken);

                ch.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch
            {
                // at-least-once: requeue
                ch.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        ch.BasicConsume(queue: q.QueueName, autoAck: false, consumer: consumer);

        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static Guid? TryParseGuid(string? s) => Guid.TryParse(s, out var g) ? g : null;
}
