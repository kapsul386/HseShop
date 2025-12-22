namespace PaymentsService.Persistence;

public sealed class Account
{
    public string UserId { get; set; } = default!;
    public decimal Balance { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}