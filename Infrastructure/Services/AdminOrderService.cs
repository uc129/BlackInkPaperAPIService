using Application.DTOs.Checkout;
using Application.DTOs.Products;
using Common.YourProject.Models;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Mappers;

namespace Infrastructure.Services;

public class AdminOrderService(IOrderRepository orderRepository) : IAdminOrderService
{
    private static readonly HashSet<string> ValidStatuses =
        ["PendingPayment", "Confirmed", "Shipped", "Delivered", "Cancelled", "Paid", "Failed"];

    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new()
    {
        ["PendingPayment"] = ["Confirmed", "Cancelled"],
        ["Paid"]           = ["Confirmed", "Cancelled"],
        ["Confirmed"]      = ["Shipped"],
        ["Shipped"]        = ["Delivered"],
        ["Delivered"]      = [],
        ["Cancelled"]      = [],
        ["Failed"]         = []
    };

    public async Task<ServiceResponse<PagedResultDto<OrderSummaryDto>>> GetAllOrdersAsync(
        AdminOrderSearchRequest request, CancellationToken ct = default)
    {
        try
        {
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var (orders, total) = await orderRepository.GetAllAsync(
                page, pageSize,
                request.Status, request.UserId,
                request.DateFrom, request.DateTo, ct);

            var items = orders.Select(o => new OrderSummaryDto(
                o.Id,
                o.OrderNumber,
                o.UserId,
                o.Status,
                o.PaymentStatus,
                o.CurrencyCode,
                o.TotalAmount,
                o.ItemCount ?? 0,
                o.CreatedAt,
                o.PaidAt,
                o.CustomerName ?? string.Empty,
                o.CustomerEmail ?? string.Empty)).ToList();

            return ServiceResponse<PagedResultDto<OrderSummaryDto>>.Ok(
                new PagedResultDto<OrderSummaryDto>(items, page, pageSize, total));
        }
        catch (Exception ex)
        {
            return ServiceResponse<PagedResultDto<OrderSummaryDto>>.Fail(
                "Unable to fetch orders.", ex.ToString(), 500, "admin_order_list_failed");
        }
    }

    public async Task<ServiceResponse<OrderDto>> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
    {
        try
        {
            var order = await orderRepository.GetById(orderId);
            if (order is null)
                return ServiceResponse<OrderDto>.Fail("Order not found.", statusCode: 404, errorCode: "order_not_found");

            return ServiceResponse<OrderDto>.Ok(CheckoutDtoMapper.ToDto(order));
        }
        catch (Exception ex)
        {
            return ServiceResponse<OrderDto>.Fail("Unable to fetch order.", ex.ToString(), 500, "admin_order_read_failed");
        }
    }

    public async Task<ServiceResponse<OrderDto>> UpdateOrderStatusAsync(
        int orderId, UpdateOrderStatusRequest request, CancellationToken ct = default)
    {
        if (!ValidStatuses.Contains(request.Status))
            return ServiceResponse<OrderDto>.Fail($"'{request.Status}' is not a valid order status.", statusCode: 400, errorCode: "invalid_status");

        try
        {
            var order = await orderRepository.GetById(orderId);
            if (order is null)
                return ServiceResponse<OrderDto>.Fail("Order not found.", statusCode: 404, errorCode: "order_not_found");

            if (!AllowedTransitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(request.Status))
                return ServiceResponse<OrderDto>.Fail(
                    $"Cannot transition from '{order.Status}' to '{request.Status}'.",
                    statusCode: 422, errorCode: "invalid_status_transition");

            var updated = await orderRepository.UpdateStatusAsync(orderId, request.Status, DateTime.UtcNow, ct);
            if (!updated)
                return ServiceResponse<OrderDto>.Fail("Status update failed.", statusCode: 500, errorCode: "status_update_failed");

            var refreshed = await orderRepository.GetById(orderId);
            return ServiceResponse<OrderDto>.Ok(CheckoutDtoMapper.ToDto(refreshed!), "Order status updated.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<OrderDto>.Fail("Unable to update order status.", ex.ToString(), 500, "status_update_failed");
        }
    }
}
