using System.Text;
using Contracts;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Persistence;
using RabbitMQ.Client;

namespace PaymentsService.Infrastructure.Messaging.Outbox;

public sealed class PaymentsOutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitConnection _rabbit;

    public PaymentsOutboxPublisher(IServiceScopeFactory scopeFactory, IRabbitConnection rabbit)
    {
        _scopeFactory = scopeFactory;
        _rabbit = rabbit;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var ch = _rabbit.CreateChannel();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

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

                        var props = ch.CreateBasicProperties();
                        props.Persistent = true;
                        props.MessageId = msg.Id.ToString();
                        props.Type = msg.Type;
                        props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                        ch.BasicPublish(
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
}
