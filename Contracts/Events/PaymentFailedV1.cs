namespace Contracts;

public sealed record PaymentFailedV1(Guid OrderId, string UserId, decimal Amount, string Reason);