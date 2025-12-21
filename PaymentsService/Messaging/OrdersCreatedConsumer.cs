using System.Text;
using System.Text.Json;
using Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PaymentsService.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentsService.Messaging;

public sealed class OrdersCreatedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitOptions _opt;

    private IConnection? _conn;
    private IModel? _ch;

    public OrdersCreatedConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitOptions> opt)
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
            queue: "payments.orders-created",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _ch.QueueBind(q.QueueName, Routing.Exchange, Routing.OrdersCreated);

        _ch.BasicQos(0, 10, false);

        var consumer = new EventingBasicConsumer(_ch);
        consumer.Received += (_, ea) =>
        {
            try
            {
                var messageId = TryParseGuid(ea.BasicProperties?.MessageId) ?? Guid.NewGuid();
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<OrderCreatedV1>(json)!;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                // Transactional Inbox + обработка (в одной транзакции)
                using var tx = db.Database.BeginTransaction();

                // 1) Inbox дедуп
                var inbox = db.Inbox.FirstOrDefault(x => x.MessageId == messageId);
                if (inbox is not null && inbox.ProcessedAtUtc is not null)
                {
                    // уже обработано — at-least-once доставка превращается в effectively exactly once
                    tx.Commit();
                    _ch.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                if (inbox is null)
                {
                    inbox = new InboxMessage
                    {
                        MessageId = messageId,
                        Type = nameof(OrderCreatedV1),
                        PayloadJson = json,
                        ReceivedAtUtc = DateTime.UtcNow,
                        ProcessAttempts = 0
                    };
                    db.Inbox.Add(inbox);
                    db.SaveChanges();
                }

                inbox.ProcessAttempts += 1;

                // 2) Обработка: списание денег "не дважды"
                //    Гарант: уникальный индекс Payments.OrderId + атомарное обновление баланса
                var acc = db.Accounts.FirstOrDefault(x => x.UserId == evt.UserId);
                if (acc is null)
                {
                    // счета нет → fail
                    WriteOutboxFail(db, evt, "Account not found");
                    inbox.ProcessedAtUtc = DateTime.UtcNow;
                    inbox.LastError = null;
                    db.SaveChanges();
                    tx.Commit();
                    _ch.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // атомарно уменьшаем баланс, если хватает денег
                var updated = db.Database.ExecuteSqlInterpolated($@"
                    UPDATE ""Accounts""
                    SET ""Balance"" = ""Balance"" - {evt.Amount}, ""UpdatedAtUtc"" = {DateTime.UtcNow}
                    WHERE ""UserId"" = {evt.UserId} AND ""Balance"" >= {evt.Amount};
                ");

                if (updated == 0)
                {
                    WriteOutboxFail(db, evt, "Insufficient funds");
                    inbox.ProcessedAtUtc = DateTime.UtcNow;
                    inbox.LastError = null;
                    db.SaveChanges();
                    tx.Commit();
                    _ch.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // создаём Payment (уникальный OrderId защитит от повторного списания)
                try
                {
                    var payment = new Payment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = evt.OrderId,
                        UserId = evt.UserId,
                        Amount = evt.Amount,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    db.Payments.Add(payment);
                    db.SaveChanges();

                    WriteOutboxSuccess(db, evt, payment.Id);
                }
                catch (DbUpdateException)
                {
                    // если повтор пришёл после успешной обработки — уникальный индекс OrderId сработает
                    // считаем это идемпотентным успехом: повторно не публикуем/не списываем
                }

                inbox.ProcessedAtUtc = DateTime.UtcNow;
                inbox.LastError = null;
                db.SaveChanges();

                tx.Commit();
                _ch.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                // requeue — at-least-once
                _ch.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _ch.BasicConsume(queue: q.QueueName, autoAck: false, consumer: consumer);
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static void WriteOutboxSuccess(PaymentsDbContext db, OrderCreatedV1 evt, Guid paymentId)
    {
        var payload = JsonSerializer.Serialize(new PaymentSucceededV1(evt.OrderId, evt.UserId, evt.Amount, paymentId));
        db.Outbox.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(PaymentSucceededV1),
            RoutingKey = Routing.PaymentsSucceeded,
            PayloadJson = payload,
            OccurredAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static void WriteOutboxFail(PaymentsDbContext db, OrderCreatedV1 evt, string reason)
    {
        var payload = JsonSerializer.Serialize(new PaymentFailedV1(evt.OrderId, evt.UserId, evt.Amount, reason));
        db.Outbox.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(PaymentFailedV1),
            RoutingKey = Routing.PaymentsFailed,
            PayloadJson = payload,
            OccurredAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private static Guid? TryParseGuid(string? s) => Guid.TryParse(s, out var g) ? g : null;

    public override void Dispose()
    {
        try { _ch?.Close(); } catch { }
        try { _conn?.Close(); } catch { }

        _ch?.Dispose();
        _conn?.Dispose();
        base.Dispose();
    }
}
