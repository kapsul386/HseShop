namespace PaymentsService.Persistence;

public sealed class InboxMessage
{
    public Guid MessageId { get; set; }
    public string Type { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public DateTime ReceivedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }
    public string? LastError { get; set; }
    public int ProcessAttempts { get; set; }
}