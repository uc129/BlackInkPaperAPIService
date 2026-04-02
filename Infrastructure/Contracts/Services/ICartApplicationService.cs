using Application.DTOs.Cart;
using Common.YourProject.Models;

namespace Infrastructure.Contracts.Services;

public interface ICartApplicationService
{
    Task<ServiceResponse<CartResponseDto>> GetActiveCartAsync(string userId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<CartResponseDto>> AddItemAsync(string userId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<CartResponseDto>> UpdateItemQuantityAsync(string userId, int cartItemId, UpdateCartItemQuantityRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<CartResponseDto>> RemoveItemAsync(string userId, int cartItemId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<CartResponseDto>> ClearAsync(string userId, CancellationToken cancellationToken = default);
}
