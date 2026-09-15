using Domain.Aggregates.Ecommerce;

namespace Infrastructure.Contracts.Services;

public enum OrderNotificationEvent
{
    Confirmed,
    PaymentFailed,
    Shipped,
    Delivered,
    Cancelled
}

public interface IOrderNotificationService
{
    /// <summary>
    /// Queues a customer notification for an order event.
    ///
    /// Implementations must never throw: this is called from inside payment capture and admin
    /// status changes, and a messaging problem must not roll back an order that really was
    /// paid for.
    ///
    /// Callers pass CancellationToken.None rather than the request's token, for the same
    /// reason. Queueing is the durable record that the customer still needs to be told; an
    /// admin closing their browser mid-request must not be what decides they never hear about
    /// their shipment.
    /// </summary>
    Task EnqueueAsync(OrderAggregate order, OrderNotificationEvent notificationEvent, CancellationToken ct = default);
}
