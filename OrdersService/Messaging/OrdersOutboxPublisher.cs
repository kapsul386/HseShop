using System.Text;
using Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrdersService.Persistence;
using RabbitMQ.Client;

namespace OrdersService.Messaging;

public sealed class OrdersOutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitOptions _opt;

    private IConnection? _conn;
    private IModel? _ch;

    public OrdersOutboxPublisher(IServiceScopeFactory scopeFactory, IOptions<RabbitOptions> opt)
    {
        _scopeFactory = scopeFactory;
        _opt = opt.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                var batch = await db.Outbox
                    .Where(x => x.PublishedAtUtc == null)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (batch.Count == 0)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                foreach (var msg in batch)
                {
                    try
                    {
                        var body = Encoding.UTF8.GetBytes(msg.PayloadJson);

                        var props = _ch.CreateBasicProperties();
                        props.Persistent = true;
                        props.MessageId = msg.Id.ToString();
                        props.Type = msg.Type;
                        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                        _ch.BasicPublish(
                            exchange: Routing.Exchange,
                            routingKey: msg.RoutingKey,
                            basicProperties: props,
                            body: body);

                        msg.PublishedAtUtc = DateTime.UtcNow;
                        msg.PublishAttempts += 1;
                        msg.LastError = null;
                    }
                    catch (Exception ex)
                    {
                        msg.PublishAttempts += 1;
                        msg.LastError = ex.Message;
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
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
