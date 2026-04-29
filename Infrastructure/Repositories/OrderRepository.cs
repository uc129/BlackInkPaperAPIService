using Dapper;
using Domain.Aggregates.Ecommerce;
using Domain.Entities.Ecommerce;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Persistence;

namespace Infrastructure.Repositories;

public class OrderRepository(IDapperContext dapperContext) : IOrderRepository
{
    public async Task<int> Add(OrderAggregate order)
    {
        const string insertOrderSql = """
            INSERT INTO Orders
            (
                OrderNumber,
                UserId,
                ShippingAddressId,
                CurrencyCode,
                Status,
                PaymentStatus,
                PaymentProvider,
                RazorpayOrderId,
                RazorpayPaymentId,
                RazorpaySignature,
                PaymentMethod,
                PaidAt,
                PaymentFailureReason,
                Subtotal,
                ShippingAmount,
                ShippingMethod,
                ShippingLabel,
                TaxAmount,
                TaxLabel,
                TaxRatePercent,
                TotalAmount,
                Notes,
                CreatedAt,
                UpdatedAt
            )
            VALUES
            (
                @OrderNumber,
                @UserId,
                @ShippingAddressId,
                @CurrencyCode,
                @Status,
                @PaymentStatus,
                @PaymentProvider,
                @RazorpayOrderId,
                @RazorpayPaymentId,
                @RazorpaySignature,
                @PaymentMethod,
                @PaidAt,
                @PaymentFailureReason,
                @Subtotal,
                @ShippingAmount,
                @ShippingMethod,
                @ShippingLabel,
                @TaxAmount,
                @TaxLabel,
                @TaxRatePercent,
                @TotalAmount,
                @Notes,
                @CreatedAt,
                @UpdatedAt
            )
            RETURNING Id;
            """;

        const string insertOrderItemSql = """
            INSERT INTO OrderItems
            (
                OrderId,
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
                FulfillmentType
            )
            VALUES
            (
                @OrderId,
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
                @FulfillmentType
            )
            RETURNING Id;
            """;

        const string insertSelectedVariantSql = """
            INSERT INTO OrderItemSelectedVariants
            (
                OrderItemId,
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
                @OrderItemId,
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

        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var orderId = await connection.ExecuteScalarAsync<int>(insertOrderSql, order, transaction);

            foreach (var item in order.Items)
            {
                var orderItemId = await connection.ExecuteScalarAsync<int>(insertOrderItemSql, new
                {
                    OrderId = orderId,
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
                    FulfillmentType = item.FulfillmentType?.ToString()
                }, transaction);

                foreach (var variant in item.SelectedVariants)
                {
                    await connection.ExecuteAsync(insertSelectedVariantSql, new
                    {
                        OrderItemId = orderItemId,
                        variant.ProductVariantId,
                        variant.ProductVariantOptionId,
                        variant.VariantLabel,
                        variant.OptionValue,
                        variant.PriceModifier,
                        variant.AbsolutePrice,
                        variant.Sku,
                        FulfillmentType = variant.FulfillmentType?.ToString()
                    }, transaction);
                }
            }

            transaction.Commit();
            return orderId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<OrderAggregate?> GetById(int id, string userId)
        => await GetSingleInternal("o.Id = @Value AND o.UserId = @UserId", new { Value = id, UserId = userId });

    public async Task<OrderAggregate?> GetById(int id)
        => await GetSingleInternal("o.Id = @Value", new { Value = id });

    public async Task<OrderAggregate?> GetByRazorpayOrderId(string razorpayOrderId)
        => await GetSingleInternal("o.RazorpayOrderId = @Value", new { Value = razorpayOrderId });

    public async Task<IEnumerable<OrderAggregate>> GetByUserId(string userId)
        => await GetOrdersListInternal("o.UserId = @UserId", new { UserId = userId });

    public async Task<bool> MarkPaymentPending(int orderId, string paymentProvider, string razorpayOrderId, DateTime updatedAt)
    {
        const string sql = """
            UPDATE Orders
            SET
                PaymentProvider = @PaymentProvider,
                RazorpayOrderId = @RazorpayOrderId,
                PaymentStatus = 'Pending',
                UpdatedAt = @UpdatedAt
            WHERE Id = @OrderId;
            """;

        using var connection = dapperContext.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new
        {
            OrderId = orderId,
            PaymentProvider = paymentProvider,
            RazorpayOrderId = razorpayOrderId,
            UpdatedAt = updatedAt
        });

        return affected > 0;
    }

    public async Task<bool> MarkPaymentAuthorized(int orderId, string razorpayPaymentId, string? razorpaySignature, string? paymentMethod, DateTime updatedAt)
    {
        const string sql = """
            UPDATE Orders
            SET
                Status = CASE WHEN Status = 'PendingPayment' THEN 'PaymentAuthorized' ELSE Status END,
                PaymentStatus = CASE WHEN PaymentStatus IN ('Pending', 'Authorized') THEN 'Authorized' ELSE PaymentStatus END,
                RazorpayPaymentId = COALESCE(@RazorpayPaymentId, RazorpayPaymentId),
                RazorpaySignature = COALESCE(@RazorpaySignature, RazorpaySignature),
                PaymentMethod = COALESCE(@PaymentMethod, PaymentMethod),
                UpdatedAt = @UpdatedAt
            WHERE Id = @OrderId;
            """;

        using var connection = dapperContext.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new
        {
            OrderId = orderId,
            RazorpayPaymentId = razorpayPaymentId,
            RazorpaySignature = razorpaySignature,
            PaymentMethod = paymentMethod,
            UpdatedAt = updatedAt
        });

        return affected > 0;
    }

    public async Task<bool> MarkPaymentCapturedAndApplyInventory(int orderId, string razorpayPaymentId, string? razorpaySignature, string? paymentMethod, DateTime paidAt, DateTime updatedAt)
    {
        using var connection = dapperContext.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var order = await GetOrderForUpdate(connection, transaction, orderId);
            if (order is null)
            {
                transaction.Rollback();
                return false;
            }

            if (string.Equals(order.PaymentStatus, "Captured", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                transaction.Commit();
                return true;
            }

            foreach (var item in order.Items)
            {
                await ApplyInventoryForOrderItem(connection, transaction, order.Id, item, updatedAt);
            }

            const string updateOrderSql = """
                UPDATE Orders
                SET
                    Status = 'Paid',
                    PaymentStatus = 'Captured',
                    RazorpayPaymentId = COALESCE(@RazorpayPaymentId, RazorpayPaymentId),
                    RazorpaySignature = COALESCE(@RazorpaySignature, RazorpaySignature),
                    PaymentMethod = COALESCE(@PaymentMethod, PaymentMethod),
                    PaidAt = @PaidAt,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @OrderId;
                """;

            await connection.ExecuteAsync(updateOrderSql, new
            {
                OrderId = orderId,
                RazorpayPaymentId = razorpayPaymentId,
                RazorpaySignature = razorpaySignature,
                PaymentMethod = paymentMethod,
                PaidAt = paidAt,
                UpdatedAt = updatedAt
            }, transaction);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> MarkPaymentFailed(int orderId, string? razorpayPaymentId, string? failureReason, DateTime updatedAt)
    {
        const string sql = """
            UPDATE Orders
            SET
                Status = CASE WHEN Status = 'PendingPayment' THEN 'PaymentFailed' ELSE Status END,
                PaymentStatus = 'Failed',
                RazorpayPaymentId = COALESCE(@RazorpayPaymentId, RazorpayPaymentId),
                PaymentFailureReason = @FailureReason,
                UpdatedAt = @UpdatedAt
            WHERE Id = @OrderId;
            """;

        using var connection = dapperContext.CreateConnection();
        var affected = await connection.ExecuteAsync(sql, new
        {
            OrderId = orderId,
            RazorpayPaymentId = razorpayPaymentId,
            FailureReason = failureReason,
            UpdatedAt = updatedAt
        });

        return affected > 0;
    }

    public async Task<bool> HasProcessedWebhookEvent(string provider, string eventId)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM PaymentWebhookEvents
                WHERE Provider = @Provider AND EventId = @EventId
            ) THEN 1 ELSE 0 END AS bit);
            """;

        using var connection = dapperContext.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Provider = provider, EventId = eventId });
    }

    public async Task RecordWebhookEvent(string provider, string eventId, string eventName, DateTime processedAt)
    {
        const string sql = """
            INSERT INTO PaymentWebhookEvents (Provider, EventId, EventName, ProcessedAt)
            VALUES (@Provider, @EventId, @EventName, @ProcessedAt);
            """;

        using var connection = dapperContext.CreateConnection();
        await connection.ExecuteAsync(sql, new { Provider = provider, EventId = eventId, EventName = eventName, ProcessedAt = processedAt });
    }

    private async Task<OrderAggregate?> GetSingleInternal(string whereClause, object parameters)
    {
        var orders = await GetOrdersListInternal(whereClause, parameters);
        return orders.FirstOrDefault();
    }

    private async Task<List<OrderAggregate>> GetOrdersListInternal(string whereClause, object parameters)
    {
        var sql = $"""
            SELECT
                o.Id,
                o.OrderNumber,
                o.UserId,
                o.ShippingAddressId,
                o.CurrencyCode,
                o.Status,
                o.PaymentStatus,
                o.PaymentProvider,
                o.RazorpayOrderId,
                o.RazorpayPaymentId,
                o.RazorpaySignature,
                o.PaymentMethod,
                o.PaidAt,
                o.PaymentFailureReason,
                o.Subtotal,
                o.ShippingAmount,
                o.ShippingMethod,
                o.ShippingLabel,
                o.TaxAmount,
                o.TaxLabel,
                o.TaxRatePercent,
                o.TotalAmount,
                o.Notes,
                o.CreatedAt,
                o.UpdatedAt,
                a.UserId AS AddressUserId,
                a.FullName,
                a.PhoneNumber,
                a.AddressLine1,
                a.AddressLine2,
                a.City,
                a.State,
                a.PostalCode,
                a.CountryCode,
                a.Landmark,
                a.IsDefault,
                a.CreatedAt AS AddressCreatedAt,
                a.UpdatedAt AS AddressUpdatedAt
            FROM Orders o
            INNER JOIN ShippingAddresses a ON a.Id = o.ShippingAddressId
            WHERE {whereClause}
            ORDER BY o.Id DESC;

            SELECT oi.*
            FROM OrderItems oi
            INNER JOIN Orders o ON o.Id = oi.OrderId
            WHERE {whereClause}
            ORDER BY oi.Id;

            SELECT osv.*
            FROM OrderItemSelectedVariants osv
            INNER JOIN OrderItems oi ON oi.Id = osv.OrderItemId
            INNER JOIN Orders o ON o.Id = oi.OrderId
            WHERE {whereClause}
            ORDER BY osv.Id;
            """;

        using var connection = dapperContext.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var orderRows = (await multi.ReadAsync<OrderWithAddressRow>()).ToList();
        var itemRows = (await multi.ReadAsync<OrderItemRow>()).ToList();
        var variantRows = (await multi.ReadAsync<OrderItemSelectedVariantRow>()).ToList();

        var orders = orderRows.Select(row => new OrderAggregate
        {
            Id = row.Id,
            OrderNumber = row.OrderNumber,
            UserId = row.UserId,
            ShippingAddressId = row.ShippingAddressId,
            CurrencyCode = row.CurrencyCode,
            Status = row.Status,
            PaymentStatus = row.PaymentStatus,
            PaymentProvider = row.PaymentProvider,
            RazorpayOrderId = row.RazorpayOrderId,
            RazorpayPaymentId = row.RazorpayPaymentId,
            RazorpaySignature = row.RazorpaySignature,
            PaymentMethod = row.PaymentMethod,
            PaidAt = row.PaidAt,
            PaymentFailureReason = row.PaymentFailureReason,
            Subtotal = row.Subtotal,
            ShippingAmount = row.ShippingAmount,
            ShippingMethod = row.ShippingMethod,
            ShippingLabel = row.ShippingLabel,
            TaxAmount = row.TaxAmount,
            TaxLabel = row.TaxLabel,
            TaxRatePercent = row.TaxRatePercent,
            TotalAmount = row.TotalAmount,
            Notes = row.Notes,
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            ShippingAddress = new ShippingAddressAggregate
            {
                Id = row.ShippingAddressId,
                UserId = row.AddressUserId,
                FullName = row.FullName,
                PhoneNumber = row.PhoneNumber,
                AddressLine1 = row.AddressLine1,
                AddressLine2 = row.AddressLine2,
                City = row.City,
                State = row.State,
                PostalCode = row.PostalCode,
                CountryCode = row.CountryCode,
                Landmark = row.Landmark,
                IsDefault = row.IsDefault,
                CreatedAt = row.AddressCreatedAt,
                UpdatedAt = row.AddressUpdatedAt
            }
        }).ToList();

        foreach (var order in orders)
        {
            order.Items = itemRows
                .Where(item => item.OrderId == order.Id)
                .Select(MapOrderItem)
                .ToList();

            foreach (var item in order.Items)
            {
                item.SelectedVariants = variantRows
                    .Where(variant => variant.OrderItemId == item.Id)
                    .Select(MapOrderSelectedVariant)
                    .ToList();
            }
        }

        return orders;
    }

    public async Task<(IEnumerable<OrderAggregate> Orders, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? status, string? userId,
        DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("o.Status = @Status");
            parameters.Add("Status", status);
        }
        if (!string.IsNullOrWhiteSpace(userId))
        {
            conditions.Add("o.UserId = @UserId");
            parameters.Add("UserId", userId);
        }
        if (dateFrom.HasValue)
        {
            conditions.Add("o.CreatedAt >= @DateFrom");
            parameters.Add("DateFrom", dateFrom.Value);
        }
        if (dateTo.HasValue)
        {
            conditions.Add("o.CreatedAt <= @DateTo");
            parameters.Add("DateTo", dateTo.Value);
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        var offset = (page - 1) * pageSize;
        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", offset);

        var sql = $"""
            SELECT COUNT(*) FROM Orders o {whereClause};

            SELECT
                o.Id,
                o.OrderNumber,
                o.UserId,
                o.Status,
                o.PaymentStatus,
                o.CurrencyCode,
                o.TotalAmount,
                o.PaidAt,
                o.CreatedAt,
                (SELECT COUNT(*) FROM OrderItems oi WHERE oi.OrderId = o.Id) AS ItemCount,
                COALESCE(u."FullName", '') AS CustomerName,
                COALESCE(u."Email", '') AS CustomerEmail
            FROM Orders o
            LEFT JOIN "Users" u ON u."Id" = o.UserId
            {whereClause}
            ORDER BY o.CreatedAt DESC
            LIMIT @Limit OFFSET @Offset;
            """;

        using var connection = dapperContext.CreateConnection();
        await using var multi = await connection.QueryMultipleAsync(sql, parameters);

        var totalCount = await multi.ReadSingleAsync<int>();
        var summaryRows = (await multi.ReadAsync<OrderSummaryRow>()).ToList();

        var orders = summaryRows.Select(row => new OrderAggregate
        {
            Id = row.Id,
            OrderNumber = row.OrderNumber,
            UserId = row.UserId,
            Status = row.Status,
            PaymentStatus = row.PaymentStatus,
            CurrencyCode = row.CurrencyCode,
            TotalAmount = row.TotalAmount,
            PaidAt = row.PaidAt,
            CreatedAt = row.CreatedAt,
            CustomerName = row.CustomerName,
            CustomerEmail = row.CustomerEmail,
            ItemCount = row.ItemCount
        });

        return (orders, totalCount);
    }

    public async Task<bool> UpdateStatusAsync(int orderId, string status, DateTime updatedAt, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE Orders
            SET Status = @Status, UpdatedAt = @UpdatedAt
            WHERE Id = @OrderId;
            """;

        using var connection = dapperContext.CreateConnection();
        var rows = await connection.ExecuteAsync(sql, new { OrderId = orderId, Status = status, UpdatedAt = updatedAt });
        return rows > 0;
    }

    private static async Task<OrderAggregate?> GetOrderForUpdate(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction, int orderId)
    {
        const string sql = """
            SELECT *
            FROM Orders
            WHERE Id = @OrderId;

            SELECT *
            FROM OrderItems
            WHERE OrderId = @OrderId
            ORDER BY Id;

            SELECT osv.*
            FROM OrderItemSelectedVariants osv
            INNER JOIN OrderItems oi ON oi.Id = osv.OrderItemId
            WHERE oi.OrderId = @OrderId
            ORDER BY osv.Id;
            """;

        await using var multi = await connection.QueryMultipleAsync(sql, new { OrderId = orderId }, transaction);
        var order = await multi.ReadSingleOrDefaultAsync<OrderAggregate>();
        if (order is null)
        {
            return null;
        }

        var itemRows = (await multi.ReadAsync<OrderItemRow>()).ToList();
        var variantRows = (await multi.ReadAsync<OrderItemSelectedVariantRow>()).ToList();

        order.Items = itemRows.Select(MapOrderItem).ToList();
        foreach (var item in order.Items)
        {
            item.SelectedVariants = variantRows
                .Where(variant => variant.OrderItemId == item.Id)
                .Select(MapOrderSelectedVariant)
                .ToList();
        }

        return order;
    }

    private static async Task ApplyInventoryForOrderItem(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction, int orderId, OrderItemAggregate item, DateTime createdAt)
    {
        var physicalVariants = item.SelectedVariants
            .Where(variant => variant.FulfillmentType == ProductFulfillmentType.physical)
            .ToList();

        if (physicalVariants.Count > 0)
        {
            var decrementedAnyVariant = false;

            foreach (var variant in physicalVariants)
            {
                var trackedStock = await connection.ExecuteScalarAsync<int?>(
                    "SELECT StockQuantity FROM ProductVariantOptions WHERE Id = @Id;",
                    new { Id = variant.ProductVariantOptionId },
                    transaction);

                if (!trackedStock.HasValue)
                {
                    continue;
                }

                var updated = await connection.ExecuteAsync(
                    """
                    UPDATE ProductVariantOptions
                    SET StockQuantity = StockQuantity - @Quantity
                    WHERE Id = @Id AND StockQuantity >= @Quantity;
                    """,
                    new { Id = variant.ProductVariantOptionId, Quantity = item.Quantity },
                    transaction);

                if (updated == 0)
                {
                    throw new InvalidOperationException($"Insufficient stock for variant option {variant.ProductVariantOptionId}.");
                }

                await connection.ExecuteAsync(
                    """
                    INSERT INTO InventoryTransactions
                    (
                        OrderId,
                        ProductDbId,
                        ProductVariantOptionId,
                        QuantityChange,
                        Reason,
                        CreatedAt
                    )
                    VALUES
                    (
                        @OrderId,
                        @ProductDbId,
                        @ProductVariantOptionId,
                        @QuantityChange,
                        @Reason,
                        @CreatedAt
                    );
                    """,
                    new
                    {
                        OrderId = orderId,
                        ProductDbId = item.ProductDbId,
                        ProductVariantOptionId = variant.ProductVariantOptionId,
                        QuantityChange = -item.Quantity,
                        Reason = "OrderPaymentCaptured",
                        CreatedAt = createdAt
                    },
                    transaction);

                decrementedAnyVariant = true;
            }

            if (decrementedAnyVariant)
            {
                return;
            }
        }

        if (item.FulfillmentType != ProductFulfillmentType.physical)
        {
            return;
        }

        var productStock = await connection.ExecuteScalarAsync<int?>(
            "SELECT StockQuantity FROM Products WHERE Id = @Id;",
            new { Id = item.ProductDbId },
            transaction);

        if (!productStock.HasValue)
        {
            return;
        }

        var productUpdated = await connection.ExecuteAsync(
            """
            UPDATE Products
            SET StockQuantity = StockQuantity - @Quantity
            WHERE Id = @Id AND StockQuantity >= @Quantity;
            """,
            new { Id = item.ProductDbId, Quantity = item.Quantity },
            transaction);

        if (productUpdated == 0)
        {
            throw new InvalidOperationException($"Insufficient stock for product {item.ProductDbId}.");
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO InventoryTransactions
            (
                OrderId,
                ProductDbId,
                ProductVariantOptionId,
                QuantityChange,
                Reason,
                CreatedAt
            )
            VALUES
            (
                @OrderId,
                @ProductDbId,
                NULL,
                @QuantityChange,
                @Reason,
                @CreatedAt
            );
            """,
            new
            {
                OrderId = orderId,
                ProductDbId = item.ProductDbId,
                QuantityChange = -item.Quantity,
                Reason = "OrderPaymentCaptured",
                CreatedAt = createdAt
            },
            transaction);
    }

    private static OrderItemAggregate MapOrderItem(OrderItemRow row)
    {
        return new OrderItemAggregate
        {
            Id = row.Id,
            OrderId = row.OrderId,
            ProductDbId = row.ProductDbId,
            ProductId = row.ProductId,
            Name = row.Name,
            Slug = row.Slug,
            CoverImageUrl = row.CoverImageUrl,
            CurrencyCode = row.CurrencyCode,
            BasePrice = row.BasePrice,
            UnitPrice = row.UnitPrice,
            Quantity = row.Quantity,
            LineTotal = row.LineTotal,
            Sku = row.Sku,
            FulfillmentType = Enum.TryParse<ProductFulfillmentType>(row.FulfillmentType, true, out var fulfillmentType)
                ? fulfillmentType
                : null
        };
    }

    private static OrderItemSelectedVariantAggregate MapOrderSelectedVariant(OrderItemSelectedVariantRow row)
    {
        return new OrderItemSelectedVariantAggregate
        {
            Id = row.Id,
            OrderItemId = row.OrderItemId,
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

    private sealed class OrderSummaryRow
    {
        public int Id { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string PaymentStatus { get; init; } = string.Empty;
        public string CurrencyCode { get; init; } = "INR";
        public decimal TotalAmount { get; init; }
        public DateTime? PaidAt { get; init; }
        public DateTime CreatedAt { get; init; }
        public int ItemCount { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public string CustomerEmail { get; init; } = string.Empty;
    }

    private sealed class OrderWithAddressRow
    {
        public int Id { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public int ShippingAddressId { get; init; }
        public string CurrencyCode { get; init; } = "INR";
        public string Status { get; init; } = string.Empty;
        public string PaymentStatus { get; init; } = string.Empty;
        public string? PaymentProvider { get; init; }
        public string? RazorpayOrderId { get; init; }
        public string? RazorpayPaymentId { get; init; }
        public string? RazorpaySignature { get; init; }
        public string? PaymentMethod { get; init; }
        public DateTime? PaidAt { get; init; }
        public string? PaymentFailureReason { get; init; }
        public decimal Subtotal { get; init; }
        public decimal ShippingAmount { get; init; }
        public string? ShippingMethod { get; init; }
        public string? ShippingLabel { get; init; }
        public decimal TaxAmount { get; init; }
        public string? TaxLabel { get; init; }
        public decimal? TaxRatePercent { get; init; }
        public decimal TotalAmount { get; init; }
        public string? Notes { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public string AddressUserId { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string AddressLine1 { get; init; } = string.Empty;
        public string? AddressLine2 { get; init; }
        public string City { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string CountryCode { get; init; } = "IN";
        public string? Landmark { get; init; }
        public bool IsDefault { get; init; }
        public DateTime AddressCreatedAt { get; init; }
        public DateTime AddressUpdatedAt { get; init; }
    }

    private sealed class OrderItemRow
    {
        public int Id { get; init; }
        public int OrderId { get; init; }
        public int ProductDbId { get; init; }
        public string ProductId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Slug { get; init; } = string.Empty;
        public string? CoverImageUrl { get; init; }
        public string CurrencyCode { get; init; } = "INR";
        public decimal BasePrice { get; init; }
        public decimal UnitPrice { get; init; }
        public int Quantity { get; init; }
        public decimal LineTotal { get; init; }
        public string? Sku { get; init; }
        public string? FulfillmentType { get; init; }
    }

    private sealed class OrderItemSelectedVariantRow
    {
        public int Id { get; init; }
        public int OrderItemId { get; init; }
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
