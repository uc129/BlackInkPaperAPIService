using Application.DTOs.Checkout;
using Application.DTOs.Products;
using Common.YourProject.Models;

namespace Infrastructure.Contracts.Services;

public interface IAdminOrderService
{
    Task<ServiceResponse<PagedResultDto<OrderSummaryDto>>> GetAllOrdersAsync(AdminOrderSearchRequest request, CancellationToken ct = default);
    Task<ServiceResponse<OrderDto>> GetOrderByIdAsync(int orderId, CancellationToken ct = default);
    Task<ServiceResponse<OrderDto>> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request, CancellationToken ct = default);
}
