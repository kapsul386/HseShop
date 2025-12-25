namespace PaymentsService.Persistence;

/// <summary>
/// Сообщение transactional outbox.
/// Гарантирует публикацию событий в RabbitMQ.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string RoutingKey { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime OccurredAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }
    public int PublishAttempts { get; set; }
    public string? LastError { get; set; }
}