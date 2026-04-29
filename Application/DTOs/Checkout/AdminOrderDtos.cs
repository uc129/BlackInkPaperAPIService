namespace Application.DTOs.Checkout;

public record AdminOrderSearchRequest(
    int Page = 1,
    int PageSize = 50,
    string? Status = null,
    string? UserId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null);

public record OrderSummaryDto(
    int Id,
    string OrderNumber,
    string UserId,
    string Status,
    string PaymentStatus,
    string CurrencyCode,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string CustomerName,
    string CustomerEmail);

public record UpdateOrderStatusRequest(string Status);
