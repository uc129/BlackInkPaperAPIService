using Domain.Aggregates.Ecommerce;

namespace Infrastructure.Contracts.Repositories;

public interface IShippingAddressRepository
{
    Task<IEnumerable<ShippingAddressAggregate>> GetByUserId(string userId);
    Task<ShippingAddressAggregate?> GetById(int id, string userId);
    Task<int> Add(ShippingAddressAggregate address);
    Task Update(ShippingAddressAggregate address);
    Task Delete(int id, string userId);
    Task ClearDefault(string userId);
}
