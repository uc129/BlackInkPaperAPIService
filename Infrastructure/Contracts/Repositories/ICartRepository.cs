using Domain.Aggregates.Ecommerce;

namespace Infrastructure.Contracts.Repositories;

public interface ICartRepository
{
    Task<CartAggregate?> GetActiveCart(string userId);
    Task<int> CreateCart(string userId, string currencyCode, DateTime createdAt);
    Task<int> AddItem(int cartId, CartItemAggregate item);
    Task UpdateItem(int cartId, CartItemAggregate item);
    Task UpdateItemQuantity(int cartId, int cartItemId, int quantity, decimal lineTotal, DateTime updatedAt);
    Task RemoveItem(int cartId, int cartItemId);
    Task ClearCart(int cartId);
}
