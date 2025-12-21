namespace Contracts;

public record PaymentRequested(Guid OrderId, string UserId, decimal Amount, string Description);

public record PaymentSucceeded(Guid OrderId, string UserId);

public record PaymentFailed(Guid OrderId, string UserId, string Reason);