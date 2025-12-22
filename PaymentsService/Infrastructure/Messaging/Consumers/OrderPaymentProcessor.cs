using System.Text.Json;
using Contracts;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Persistence;

namespace PaymentsService.Infrastructure.Messaging.Consumers;

public sealed class OrderPaymentProcessor
{
    private readonly PaymentsDbContext _db;

    public OrderPaymentProcessor(PaymentsDbContext db) => _db = db;

    public async Task ProcessAsync(Guid messageId, OrderCreatedV1 evt, string rawJson, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1) Inbox дедуп
        var inbox = await _db.Inbox.FirstOrDefaultAsync(x => x.MessageId == messageId, ct);
        if (inbox is not null && inbox.ProcessedAtUtc is not null)
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (inbox is null)
        {
            inbox = new InboxMessage
            {
                MessageId = messageId,
                Type = nameof(OrderCreatedV1),
                PayloadJson = rawJson,
                ReceivedAtUtc = DateTime.UtcNow,
                ProcessAttempts = 0
            };
            _db.Inbox.Add(inbox);
            await _db.SaveChangesAsync(ct);
        }

        inbox.ProcessAttempts += 1;

        // 2) Счет существует?
        var acc = await _db.Accounts.FirstOrDefaultAsync(x => x.UserId == evt.UserId, ct);
        if (acc is null)
        {
            await WriteOutboxFailAsync(evt, "Account not found", ct);
            inbox.ProcessedAtUtc = DateTime.UtcNow;
            inbox.LastError = null;
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return;
        }

        // 3) Атомарное списание (если хватает денег)
        var updated = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Accounts""
            SET ""Balance"" = ""Balance"" - {evt.Amount}, ""UpdatedAtUtc"" = {DateTime.UtcNow}
            WHERE ""UserId"" = {evt.UserId} AND ""Balance"" >= {evt.Amount};
        ", ct);

        if (updated == 0)
        {
            await WriteOutboxFailAsync(evt, "Insufficient funds", ct);
            inbox.ProcessedAtUtc = DateTime.UtcNow;
            inbox.LastError = null;
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return;
        }

        // 4) Создаём Payment (уникальный индекс OrderId защищает от повторного списания)
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
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync(ct);

            await WriteOutboxSuccessAsync(evt, payment.Id, ct);
        }
        catch (DbUpdateException)
        {
            // Повторное сообщение после успешной обработки: OrderId уникален — считаем идемпотентным успехом.
            // Outbox повторно не пишем.
        }

        inbox.ProcessedAtUtc = DateTime.UtcNow;
        inbox.LastError = null;
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
    }

    private async Task WriteOutboxSuccessAsync(OrderCreatedV1 evt, Guid paymentId, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new PaymentSucceededV1(evt.OrderId, evt.UserId, evt.Amount, paymentId));
        _db.Outbox.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(PaymentSucceededV1),
            RoutingKey = Routing.PaymentsSucceeded,
            PayloadJson = payload,
            OccurredAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task WriteOutboxFailAsync(OrderCreatedV1 evt, string reason, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(new PaymentFailedV1(evt.OrderId, evt.UserId, evt.Amount, reason));
        _db.Outbox.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(PaymentFailedV1),
            RoutingKey = Routing.PaymentsFailed,
            PayloadJson = payload,
            OccurredAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
