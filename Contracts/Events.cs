namespace Contracts;

public static class Routing
{
    public const string Exchange = "hse.shop";

    public const string OrdersCreated = "orders.created";
    public const string PaymentsSucceeded = "payments.succeeded";
    public const string PaymentsFailed = "payments.failed";
}

public record MessageEnvelope(
    Guid MessageId,
    string Type,
    DateTime OccurredAtUtc,
    string PayloadJson,
    Guid? CorrelationId = null
);

public record OrderCreatedV1(Guid OrderId, string UserId, decimal Amount);

public record PaymentSucceededV1(Guid OrderId, string UserId, decimal Amount, Guid PaymentId);

public record PaymentFailedV1(Guid OrderId, string UserId, decimal Amount, string Reason);