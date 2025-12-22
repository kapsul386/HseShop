namespace Contracts;

public sealed record OrderCreatedV1(Guid OrderId, string UserId, decimal Amount);