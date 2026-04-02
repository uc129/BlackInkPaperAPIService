using Dapper;
using Domain.Aggregates.Ecommerce;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class ShippingAddressRepository(IDapperContext dapperContext) : IShippingAddressRepository
{
    public async Task<IEnumerable<ShippingAddressAggregate>> GetByUserId(string userId)
    {
        const string sql = """
            SELECT *
            FROM ShippingAddresses
            WHERE UserId = @UserId
            ORDER BY IsDefault DESC, Id DESC;
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.QueryAsync<ShippingAddressAggregate>(sql, new { UserId = userId });
    }

    public async Task<ShippingAddressAggregate?> GetById(int id, string userId)
    {
        const string sql = """
            SELECT *
            FROM ShippingAddresses
            WHERE Id = @Id AND UserId = @UserId;
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ShippingAddressAggregate>(sql, new { Id = id, UserId = userId });
    }

    public async Task<int> Add(ShippingAddressAggregate address)
    {
        const string sql = """
            INSERT INTO ShippingAddresses
            (
                UserId,
                FullName,
                PhoneNumber,
                AddressLine1,
                AddressLine2,
                City,
                State,
                PostalCode,
                CountryCode,
                Landmark,
                IsDefault,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                @UserId,
                @FullName,
                @PhoneNumber,
                @AddressLine1,
                @AddressLine2,
                @City,
                @State,
                @PostalCode,
                @CountryCode,
                @Landmark,
                @IsDefault,
                @CreatedAt,
                @UpdatedAt
            );

            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, address);
    }

    public async Task Update(ShippingAddressAggregate address)
    {
        const string sql = """
            UPDATE ShippingAddresses
            SET
                FullName = @FullName,
                PhoneNumber = @PhoneNumber,
                AddressLine1 = @AddressLine1,
                AddressLine2 = @AddressLine2,
                City = @City,
                State = @State,
                PostalCode = @PostalCode,
                CountryCode = @CountryCode,
                Landmark = @Landmark,
                IsDefault = @IsDefault,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND UserId = @UserId;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, address);
    }

    public async Task Delete(int id, string userId)
    {
        const string sql = """
            DELETE FROM ShippingAddresses
            WHERE Id = @Id AND UserId = @UserId;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id, UserId = userId });
    }

    public async Task ClearDefault(string userId)
    {
        const string sql = """
            UPDATE ShippingAddresses
            SET IsDefault = 0
            WHERE UserId = @UserId;
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId });
    }
}
