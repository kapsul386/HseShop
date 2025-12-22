namespace Contracts;

public sealed record PaymentSucceededV1(Guid OrderId, string UserId, decimal Amount, Guid PaymentId);