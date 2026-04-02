using Application.DTOs.Checkout;
using Common.YourProject.Models;

namespace Infrastructure.Contracts.Services;

public interface ICheckoutApplicationService
{
    Task<ServiceResponse<IReadOnlyList<ShippingAddressDto>>> GetAddressesAsync(string userId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ShippingAddressDto>> AddAddressAsync(string userId, CreateShippingAddressRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<ShippingAddressDto>> UpdateAddressAsync(string userId, int id, UpdateShippingAddressRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> DeleteAddressAsync(string userId, int id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<CheckoutPreviewDto>> PreviewAsync(string userId, CheckoutPreviewRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PaymentSessionDto>> CreatePaymentSessionAsync(string userId, CreatePaymentSessionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<OrderDto>> VerifyPaymentAsync(string userId, VerifyRazorpayPaymentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<bool>> HandleRazorpayWebhookAsync(string rawBody, string? signature, string? eventId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<PlaceOrderResponseDto>> PlaceOrderAsync(string userId, PlaceOrderRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<IReadOnlyList<OrderDto>>> GetOrdersAsync(string userId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<OrderDto>> GetOrderByIdAsync(string userId, int orderId, CancellationToken cancellationToken = default);
}
