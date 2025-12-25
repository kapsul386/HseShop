namespace PaymentsService.Persistence;

/// <summary>
/// Сообщение transactional inbox.
/// Используется для дедупликации входящих событий.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>
    /// Идентификатор сообщения (MessageId из брокера).
    /// </summary>
    public Guid MessageId { get; set; }

    public string Type { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime ReceivedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }
    public string? LastError { get; set; }
    public int ProcessAttempts { get; set; }
}