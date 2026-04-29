using Domain.Aggregates.Ecommerce;

namespace Infrastructure.Contracts.Repositories;

public interface IOrderRepository
{
    Task<int> Add(OrderAggregate order);
    Task<OrderAggregate?> GetById(int id, string userId);
    Task<OrderAggregate?> GetById(int id);
    Task<OrderAggregate?> GetByRazorpayOrderId(string razorpayOrderId);
    Task<IEnumerable<OrderAggregate>> GetByUserId(string userId);
    Task<bool> MarkPaymentPending(int orderId, string paymentProvider, string razorpayOrderId, DateTime updatedAt);
    Task<bool> MarkPaymentAuthorized(int orderId, string razorpayPaymentId, string? razorpaySignature, string? paymentMethod, DateTime updatedAt);
    Task<bool> MarkPaymentCapturedAndApplyInventory(int orderId, string razorpayPaymentId, string? razorpaySignature, string? paymentMethod, DateTime paidAt, DateTime updatedAt);
    Task<bool> MarkPaymentFailed(int orderId, string? razorpayPaymentId, string? failureReason, DateTime updatedAt);
    Task<bool> HasProcessedWebhookEvent(string provider, string eventId);
    Task RecordWebhookEvent(string provider, string eventId, string eventName, DateTime processedAt);
    Task<(IEnumerable<OrderAggregate> Orders, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? status, string? userId,
        DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(int orderId, string status, DateTime updatedAt, CancellationToken ct = default);
}
