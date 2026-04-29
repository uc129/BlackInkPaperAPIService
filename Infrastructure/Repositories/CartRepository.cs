using Dapper;
using Domain.Aggregates.Ecommerce;
using Domain.Entities.Ecommerce;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class CartRepository(IDapperContext dapperContext) : ICartRepository
{
    public async Task<CartAggregate?> GetActiveCart(string userId)
    {
        const string sql = """
            SELECT TOP 1 *
            FROM Carts
            WHERE UserId = @UserId AND Status = 'Active'
            ORDER BY Id DESC;

            SELECT items.*
            FROM CartItems items
            INNER JOIN Carts carts ON carts.Id = items.CartId
            WHERE carts.UserId = @UserId AND carts.Status = 'Active'
            ORDER BY items.Id;

            SELECT variants.*
            FROM CartItemSelectedVariants variants
            INNER JOIN CartItems items ON items.Id = variants.CartItemId
            INNER JOIN Carts carts ON carts.Id = items.CartId
            WHERE carts.UserId = @UserId AND carts.Status = 'Active'
            ORDER BY variants.Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, new { UserId = userId });

        var cart = await multi.ReadSingleOrDefaultAsync<CartAggregate>();
        if (cart is null)
        {
            return null;
        }

        var items = (await multi.ReadAsync<CartItemAggregate>()).ToList();
        var selectedVariants = (await multi.ReadAsync<CartItemSelectedVariantRow>()).ToList();

        foreach (var item in items)
        {
            item.SelectedVariants = selectedVariants
                .Where(variant => variant.CartItemId == item.Id)
                .Select(MapSelectedVariant)
                .ToList();
        }

        cart.Items = items;
        return cart;
    }

    public async Task<int> CreateCart(string userId, string currencyCode, DateTime createdAt)
    {
        const string sql = """
            INSERT INTO Carts (UserId, CurrencyCode, Status, CreatedAt, UpdatedAt)
            VALUES (@UserId, @CurrencyCode, 'Active', @CreatedAt, @UpdatedAt)
            RETURNING Id;
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            UserId = userId,
            CurrencyCode = currencyCode,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        });
    }

    public async Task<int> AddItem(int cartId, CartItemAggregate item)
    {
        const string sql = """
            INSERT INTO CartItems
            (
                CartId,
                ProductDbId,
                ProductId,
                Name,
                Slug,
                CoverImageUrl,
                CurrencyCode,
                BasePrice,
                UnitPrice,
                Quantity,
                LineTotal,
                Sku,
                FulfillmentType,
                AddedAt,
                UpdatedAt
            )
            VALUES
            (
                @CartId,
                @ProductDbId,
                @ProductId,
                @Name,
                @Slug,
                @CoverImageUrl,
                @CurrencyCode,
                @BasePrice,
                @UnitPrice,
                @Quantity,
                @LineTotal,
                @Sku,
                @FulfillmentType,
                @AddedAt,
                @UpdatedAt
            )
            RETURNING Id;
            """;

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var cartItemId = await connection.ExecuteScalarAsync<int>(sql, new
            {
                CartId = cartId,
                item.ProductDbId,
                item.ProductId,
                item.Name,
                item.Slug,
                item.CoverImageUrl,
                item.CurrencyCode,
                item.BasePrice,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal,
                item.Sku,
                FulfillmentType = item.FulfillmentType?.ToString(),
                item.AddedAt,
                item.UpdatedAt
            }, transaction);

            await ReplaceSelectedVariants(connection, transaction, cartItemId, item.SelectedVariants);
            await TouchCart(connection, transaction, cartId, item.UpdatedAt);

            transaction.Commit();
            return cartItemId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateItem(int cartId, CartItemAggregate item)
    {
        const string sql = """
            UPDATE CartItems
            SET
                ProductDbId = @ProductDbId,
                ProductId = @ProductId,
                Name = @Name,
                Slug = @Slug,
                CoverImageUrl = @CoverImageUrl,
                CurrencyCode = @CurrencyCode,
                BasePrice = @BasePrice,
                UnitPrice = @UnitPrice,
                Quantity = @Quantity,
                LineTotal = @LineTotal,
                Sku = @Sku,
                FulfillmentType = @FulfillmentType,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id AND CartId = @CartId;
            """;

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(sql, new
            {
                item.Id,
                CartId = cartId,
                item.ProductDbId,
                item.ProductId,
                item.Name,
                item.Slug,
                item.CoverImageUrl,
                item.CurrencyCode,
                item.BasePrice,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal,
                item.Sku,
                FulfillmentType = item.FulfillmentType?.ToString(),
                item.UpdatedAt
            }, transaction);

            await ReplaceSelectedVariants(connection, transaction, item.Id, item.SelectedVariants);
            await TouchCart(connection, transaction, cartId, item.UpdatedAt);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateItemQuantity(int cartId, int cartItemId, int quantity, decimal lineTotal, DateTime updatedAt)
    {
        const string sql = """
            UPDATE CartItems
            SET
                Quantity = @Quantity,
                LineTotal = @LineTotal,
                UpdatedAt = @UpdatedAt
            WHERE Id = @CartItemId AND CartId = @CartId;
            """;

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(sql, new
            {
                CartId = cartId,
                CartItemId = cartItemId,
                Quantity = quantity,
                LineTotal = lineTotal,
                UpdatedAt = updatedAt
            }, transaction);

            await TouchCart(connection, transaction, cartId, updatedAt);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task RemoveItem(int cartId, int cartItemId)
    {
        const string sql = """
            DELETE FROM CartItemSelectedVariants
            WHERE CartItemId = @CartItemId;

            DELETE FROM CartItems
            WHERE Id = @CartItemId AND CartId = @CartId;
            """;

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(sql, new { CartId = cartId, CartItemId = cartItemId }, transaction);
            await TouchCart(connection, transaction, cartId, DateTime.UtcNow);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task ClearCart(int cartId)
    {
        const string sql = """
            DELETE FROM CartItemSelectedVariants
            WHERE CartItemId IN (SELECT Id FROM CartItems WHERE CartId = @CartId);

            DELETE FROM CartItems
            WHERE CartId = @CartId;
            """;

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(sql, new { CartId = cartId }, transaction);
            await TouchCart(connection, transaction, cartId, DateTime.UtcNow);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task ReplaceSelectedVariants(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        int cartItemId,
        IEnumerable<CartItemSelectedVariant> selectedVariants)
    {
        const string deleteSql = """
            DELETE FROM CartItemSelectedVariants
            WHERE CartItemId = @CartItemId;
            """;

        const string insertSql = """
            INSERT INTO CartItemSelectedVariants
            (
                CartItemId,
                ProductVariantId,
                ProductVariantOptionId,
                VariantLabel,
                OptionValue,
                PriceModifier,
                AbsolutePrice,
                Sku,
                FulfillmentType
            )
            VALUES
            (
                @CartItemId,
                @ProductVariantId,
                @ProductVariantOptionId,
                @VariantLabel,
                @OptionValue,
                @PriceModifier,
                @AbsolutePrice,
                @Sku,
                @FulfillmentType
            );
            """;

        await connection.ExecuteAsync(deleteSql, new { CartItemId = cartItemId }, transaction);

        foreach (var selectedVariant in selectedVariants)
        {
            await connection.ExecuteAsync(insertSql, new
            {
                CartItemId = cartItemId,
                selectedVariant.ProductVariantId,
                selectedVariant.ProductVariantOptionId,
                selectedVariant.VariantLabel,
                selectedVariant.OptionValue,
                selectedVariant.PriceModifier,
                selectedVariant.AbsolutePrice,
                selectedVariant.Sku,
                FulfillmentType = selectedVariant.FulfillmentType?.ToString()
            }, transaction);
        }
    }

    private static async Task TouchCart(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        int cartId,
        DateTime updatedAt)
    {
        const string sql = """
            UPDATE Carts
            SET UpdatedAt = @UpdatedAt
            WHERE Id = @CartId;
            """;

        await connection.ExecuteAsync(sql, new { CartId = cartId, UpdatedAt = updatedAt }, transaction);
    }

    private static CartItemSelectedVariant MapSelectedVariant(CartItemSelectedVariantRow row)
    {
        return new CartItemSelectedVariant
        {
            Id = row.Id,
            CartItemId = row.CartItemId,
            ProductVariantId = row.ProductVariantId,
            ProductVariantOptionId = row.ProductVariantOptionId,
            VariantLabel = row.VariantLabel,
            OptionValue = row.OptionValue,
            PriceModifier = row.PriceModifier,
            AbsolutePrice = row.AbsolutePrice,
            Sku = row.Sku,
            FulfillmentType = Enum.TryParse<ProductFulfillmentType>(row.FulfillmentType, true, out var fulfillmentType)
                ? fulfillmentType
                : null
        };
    }

    private sealed class CartItemSelectedVariantRow
    {
        public int Id { get; init; }
        public int CartItemId { get; init; }
        public int ProductVariantId { get; init; }
        public int ProductVariantOptionId { get; init; }
        public string VariantLabel { get; init; } = string.Empty;
        public string OptionValue { get; init; } = string.Empty;
        public decimal? PriceModifier { get; init; }
        public decimal? AbsolutePrice { get; init; }
        public string? Sku { get; init; }
        public string? FulfillmentType { get; init; }
    }
}
