using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.Extensions.Options;
using OrdersService.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrdersService.Messaging;

public sealed class PaymentsEventsConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitOptions _opt;

    private IConnection? _conn;
    private IModel? _ch;

    public PaymentsEventsConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitOptions> opt)
    {
        _scopeFactory = scopeFactory;
        _opt = opt.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _opt.Host,
            Port = _opt.Port,
            UserName = _opt.User,
            Password = _opt.Pass
        };

        _conn = factory.CreateConnection();
        _ch = _conn.CreateModel();

        _ch.ExchangeDeclare(exchange: Routing.Exchange, type: ExchangeType.Topic, durable: true);

        var q = _ch.QueueDeclare(
            queue: "orders.payment-results",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _ch.QueueBind(q.QueueName, Routing.Exchange, Routing.PaymentsSucceeded);
        _ch.QueueBind(q.QueueName, Routing.Exchange, Routing.PaymentsFailed);

        var consumer = new EventingBasicConsumer(_ch);
        consumer.Received += (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                if (ea.RoutingKey == Routing.PaymentsSucceeded)
                {
                    var evt = JsonSerializer.Deserialize<PaymentSucceededV1>(json)!;
                    var order = db.Orders.FirstOrDefault(x => x.Id == evt.OrderId);
                    if (order != null && order.Status == OrderStatus.NEW)
                    {
                        order.Status = OrderStatus.FINISHED;
                        db.SaveChanges();
                    }
                }
                else if (ea.RoutingKey == Routing.PaymentsFailed)
                {
                    var evt = JsonSerializer.Deserialize<PaymentFailedV1>(json)!;
                    var order = db.Orders.FirstOrDefault(x => x.Id == evt.OrderId);
                    if (order != null && order.Status == OrderStatus.NEW)
                    {
                        order.Status = OrderStatus.CANCELLED;
                        order.CancelReason = evt.Reason;
                        db.SaveChanges();
                    }
                }

                _ch.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch
            {
                _ch.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _ch.BasicConsume(queue: q.QueueName, autoAck: false, consumer: consumer);

        // держим сервис живым, пока не отменят
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        try { _ch?.Close(); } catch { /* ignore */ }
        try { _conn?.Close(); } catch { /* ignore */ }

        _ch?.Dispose();
        _conn?.Dispose();

        base.Dispose();
    }
}
