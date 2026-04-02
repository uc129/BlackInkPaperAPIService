using System.Text.Json;
using Application.DTOs.Checkout;
using Common.YourProject.Models;
using Domain.Aggregates.Ecommerce;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Infrastructure.Mappers;

namespace Infrastructure.Services;

public class CheckoutApplicationService(
    ICartRepository cartRepository,
    IShippingAddressRepository shippingAddressRepository,
    IOrderRepository orderRepository,
    ICheckoutPricingService checkoutPricingService,
    IRazorpayGateway razorpayGateway) : ICheckoutApplicationService
{
    public async Task<ServiceResponse<IReadOnlyList<ShippingAddressDto>>> GetAddressesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var addresses = (await shippingAddressRepository.GetByUserId(userId))
                .Select(CheckoutDtoMapper.ToDto)
                .ToList();

            return ServiceResponse<IReadOnlyList<ShippingAddressDto>>.Ok(addresses);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IReadOnlyList<ShippingAddressDto>>.Fail("Unable to fetch shipping addresses.", ex.ToString(), 500, "shipping_address_read_failed");
        }
    }

    public async Task<ServiceResponse<ShippingAddressDto>> AddAddressAsync(string userId, CreateShippingAddressRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var validation = ValidateAddress(request.FullName, request.PhoneNumber, request.AddressLine1, request.City, request.State, request.PostalCode, request.CountryCode);
            if (!validation.Success)
            {
                return validation;
            }

            var now = DateTime.UtcNow;
            var address = CheckoutDtoMapper.ToAggregate(userId, request, now);
            if (address.IsDefault)
            {
                await shippingAddressRepository.ClearDefault(userId);
            }

            address.Id = await shippingAddressRepository.Add(address);
            return ServiceResponse<ShippingAddressDto>.Ok(CheckoutDtoMapper.ToDto(address), "Shipping address created successfully.", 201);
        }
        catch (Exception ex)
        {
            return ServiceResponse<ShippingAddressDto>.Fail("Unable to create shipping address.", ex.ToString(), 500, "shipping_address_write_failed");
        }
    }

    public async Task<ServiceResponse<ShippingAddressDto>> UpdateAddressAsync(string userId, int id, UpdateShippingAddressRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var validation = ValidateAddress(request.FullName, request.PhoneNumber, request.AddressLine1, request.City, request.State, request.PostalCode, request.CountryCode);
            if (!validation.Success)
            {
                return validation;
            }

            var existing = await shippingAddressRepository.GetById(id, userId);
            if (existing is null)
            {
                return ServiceResponse<ShippingAddressDto>.Fail("Shipping address not found.", statusCode: 404, errorCode: "shipping_address_not_found");
            }

            if (request.IsDefault)
            {
                await shippingAddressRepository.ClearDefault(userId);
            }

            CheckoutDtoMapper.ApplyUpdate(existing, request, DateTime.UtcNow);
            await shippingAddressRepository.Update(existing);

            return ServiceResponse<ShippingAddressDto>.Ok(CheckoutDtoMapper.ToDto(existing), "Shipping address updated successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<ShippingAddressDto>.Fail("Unable to update shipping address.", ex.ToString(), 500, "shipping_address_write_failed");
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAddressAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await shippingAddressRepository.GetById(id, userId);
            if (existing is null)
            {
                return ServiceResponse<bool>.Fail("Shipping address not found.", statusCode: 404, errorCode: "shipping_address_not_found");
            }

            await shippingAddressRepository.Delete(id, userId);
            return ServiceResponse<bool>.Ok(true, "Shipping address deleted successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Fail("Unable to delete shipping address.", ex.ToString(), 500, "shipping_address_write_failed");
        }
    }

    public async Task<ServiceResponse<CheckoutPreviewDto>> PreviewAsync(string userId, CheckoutPreviewRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var cart = await cartRepository.GetActiveCart(userId);
            if (cart is null || cart.Items.Count == 0)
            {
                return ServiceResponse<CheckoutPreviewDto>.Fail("Cart is empty.", statusCode: 400, errorCode: "cart_empty");
            }

            var shippingAddress = await shippingAddressRepository.GetById(request.ShippingAddressId, userId);
            if (shippingAddress is null)
            {
                return ServiceResponse<CheckoutPreviewDto>.Fail("Shipping address not found.", statusCode: 404, errorCode: "shipping_address_not_found");
            }

            var pricing = await checkoutPricingService.BuildAsync(cart, shippingAddress, cancellationToken);
            return ServiceResponse<CheckoutPreviewDto>.Ok(BuildPreview(pricing, shippingAddress));
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResponse<CheckoutPreviewDto>.Fail(ex.Message, ex.ToString(), 400, "checkout_validation_failed");
        }
        catch (Exception ex)
        {
            return ServiceResponse<CheckoutPreviewDto>.Fail("Unable to preview checkout.", ex.ToString(), 500, "checkout_preview_failed");
        }
    }

    public async Task<ServiceResponse<PaymentSessionDto>> CreatePaymentSessionAsync(string userId, CreatePaymentSessionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var cart = await cartRepository.GetActiveCart(userId);
            if (cart is null || cart.Items.Count == 0)
            {
                return ServiceResponse<PaymentSessionDto>.Fail("Cart is empty.", statusCode: 400, errorCode: "cart_empty");
            }

            var shippingAddress = await shippingAddressRepository.GetById(request.ShippingAddressId, userId);
            if (shippingAddress is null)
            {
                return ServiceResponse<PaymentSessionDto>.Fail("Shipping address not found.", statusCode: 404, errorCode: "shipping_address_not_found");
            }

            var pricing = await checkoutPricingService.BuildAsync(cart, shippingAddress, cancellationToken);
            var now = DateTime.UtcNow;
            var order = BuildOrder(userId, shippingAddress, request.Notes, pricing, now, "Razorpay");
            order.Id = await orderRepository.Add(order);

            var amountInSubunits = ToCurrencySubunits(pricing.TotalAmount);
            var razorpayOrder = await razorpayGateway.CreateOrderAsync(
                amountInSubunits,
                order.CurrencyCode,
                order.OrderNumber,
                new Dictionary<string, string>
                {
                    ["internal_order_id"] = order.Id.ToString(),
                    ["internal_order_number"] = order.OrderNumber
                },
                cancellationToken);

            await orderRepository.MarkPaymentPending(order.Id, "Razorpay", razorpayOrder.Id, DateTime.UtcNow);
            order.RazorpayOrderId = razorpayOrder.Id;

            return ServiceResponse<PaymentSessionDto>.Ok(
                new PaymentSessionDto(
                    order.Id,
                    order.OrderNumber,
                    razorpayOrder.Id,
                    razorpayGateway.KeyId,
                    order.CurrencyCode,
                    razorpayOrder.Amount,
                    "Black Ink Paper",
                    "Artwork purchase",
                    shippingAddress.FullName,
                    shippingAddress.PhoneNumber,
                    BuildPreview(pricing, shippingAddress)),
                "Payment session created successfully.",
                201);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResponse<PaymentSessionDto>.Fail(ex.Message, ex.ToString(), 400, "checkout_validation_failed");
        }
        catch (Exception ex)
        {
            return ServiceResponse<PaymentSessionDto>.Fail("Unable to create payment session.", ex.ToString(), 500, "payment_session_create_failed");
        }
    }

    public async Task<ServiceResponse<OrderDto>> VerifyPaymentAsync(string userId, VerifyRazorpayPaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await orderRepository.GetById(request.OrderId, userId);
            if (order is null)
            {
                return ServiceResponse<OrderDto>.Fail("Order not found.", statusCode: 404, errorCode: "order_not_found");
            }

            if (string.IsNullOrWhiteSpace(order.RazorpayOrderId) ||
                !string.Equals(order.RazorpayOrderId, request.RazorpayOrderId, StringComparison.Ordinal))
            {
                return ServiceResponse<OrderDto>.Fail("Payment order mismatch.", statusCode: 400, errorCode: "payment_order_mismatch");
            }

            if (!razorpayGateway.VerifyPaymentSignature(order.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature))
            {
                return ServiceResponse<OrderDto>.Fail("Payment signature verification failed.", statusCode: 400, errorCode: "invalid_payment_signature");
            }

            var payment = await razorpayGateway.FetchPaymentAsync(request.RazorpayPaymentId, cancellationToken);
            if (payment is null || !string.Equals(payment.OrderId, order.RazorpayOrderId, StringComparison.Ordinal))
            {
                return ServiceResponse<OrderDto>.Fail("Unable to validate payment with Razorpay.", statusCode: 400, errorCode: "payment_validation_failed");
            }

            var updatedAt = DateTime.UtcNow;
            if (string.Equals(payment.Status, "captured", StringComparison.OrdinalIgnoreCase))
            {
                await orderRepository.MarkPaymentCapturedAndApplyInventory(order.Id, payment.Id, request.RazorpaySignature, payment.Method, updatedAt, updatedAt);

                var cart = await cartRepository.GetActiveCart(userId);
                if (cart is not null)
                {
                    await cartRepository.ClearCart(cart.Id);
                }
            }
            else if (string.Equals(payment.Status, "authorized", StringComparison.OrdinalIgnoreCase))
            {
                await orderRepository.MarkPaymentAuthorized(order.Id, payment.Id, request.RazorpaySignature, payment.Method, updatedAt);
            }
            else
            {
                await orderRepository.MarkPaymentFailed(order.Id, payment.Id, $"Razorpay payment status: {payment.Status}", updatedAt);
            }

            var savedOrder = await orderRepository.GetById(order.Id, userId);
            if (savedOrder is null)
            {
                return ServiceResponse<OrderDto>.Fail("Order not found after payment verification.", statusCode: 404, errorCode: "order_not_found");
            }

            return ServiceResponse<OrderDto>.Ok(CheckoutDtoMapper.ToDto(savedOrder), "Payment verified successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResponse<OrderDto>.Fail(ex.Message, ex.ToString(), 400, "inventory_update_failed");
        }
        catch (Exception ex)
        {
            return ServiceResponse<OrderDto>.Fail("Unable to verify payment.", ex.ToString(), 500, "payment_verify_failed");
        }
    }

    public async Task<ServiceResponse<bool>> HandleRazorpayWebhookAsync(string rawBody, string? signature, string? eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(signature) || !razorpayGateway.VerifyWebhookSignature(rawBody, signature))
            {
                return ServiceResponse<bool>.Fail("Invalid webhook signature.", statusCode: 400, errorCode: "invalid_webhook_signature");
            }

            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            var eventName = root.TryGetProperty("event", out var eventElement) ? eventElement.GetString() ?? string.Empty : string.Empty;
            var paymentEntity = root
                .GetProperty("payload")
                .GetProperty("payment")
                .GetProperty("entity");

            var razorpayOrderId = paymentEntity.TryGetProperty("order_id", out var orderIdElement) ? orderIdElement.GetString() : null;
            var razorpayPaymentId = paymentEntity.TryGetProperty("id", out var paymentIdElement) ? paymentIdElement.GetString() : null;
            var paymentMethod = paymentEntity.TryGetProperty("method", out var methodElement) ? methodElement.GetString() : null;
            var derivedEventId = string.IsNullOrWhiteSpace(eventId)
                ? $"{eventName}:{razorpayPaymentId}:{paymentEntity.TryGetProperty("status", out var statusElement)}"
                : eventId;

            if (await orderRepository.HasProcessedWebhookEvent("Razorpay", derivedEventId))
            {
                return ServiceResponse<bool>.Ok(true, "Webhook already processed.");
            }

            if (string.IsNullOrWhiteSpace(razorpayOrderId))
            {
                await orderRepository.RecordWebhookEvent("Razorpay", derivedEventId, eventName, DateTime.UtcNow);
                return ServiceResponse<bool>.Ok(true, "Webhook ignored.");
            }

            var order = await orderRepository.GetByRazorpayOrderId(razorpayOrderId);
            if (order is null)
            {
                await orderRepository.RecordWebhookEvent("Razorpay", derivedEventId, eventName, DateTime.UtcNow);
                return ServiceResponse<bool>.Ok(true, "Webhook acknowledged.");
            }

            var now = DateTime.UtcNow;
            switch (eventName)
            {
                case "payment.authorized":
                    if (!string.IsNullOrWhiteSpace(razorpayPaymentId))
                    {
                        await orderRepository.MarkPaymentAuthorized(order.Id, razorpayPaymentId, null, paymentMethod, now);
                    }
                    break;
                case "payment.captured":
                    if (!string.IsNullOrWhiteSpace(razorpayPaymentId))
                    {
                        await orderRepository.MarkPaymentCapturedAndApplyInventory(order.Id, razorpayPaymentId, null, paymentMethod, now, now);
                    }
                    break;
                case "payment.failed":
                    await orderRepository.MarkPaymentFailed(order.Id, razorpayPaymentId, "Razorpay webhook reported payment failure.", now);
                    break;
            }

            await orderRepository.RecordWebhookEvent("Razorpay", derivedEventId, eventName, now);
            return ServiceResponse<bool>.Ok(true, "Webhook processed successfully.");
        }
        catch (Exception ex)
        {
            return ServiceResponse<bool>.Fail("Unable to process webhook.", ex.ToString(), 500, "webhook_processing_failed");
        }
    }

    public async Task<ServiceResponse<PlaceOrderResponseDto>> PlaceOrderAsync(string userId, PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var cart = await cartRepository.GetActiveCart(userId);
            if (cart is null || cart.Items.Count == 0)
            {
                return ServiceResponse<PlaceOrderResponseDto>.Fail("Cart is empty.", statusCode: 400, errorCode: "cart_empty");
            }

            var shippingAddress = await shippingAddressRepository.GetById(request.ShippingAddressId, userId);
            if (shippingAddress is null)
            {
                return ServiceResponse<PlaceOrderResponseDto>.Fail("Shipping address not found.", statusCode: 404, errorCode: "shipping_address_not_found");
            }

            var pricing = await checkoutPricingService.BuildAsync(cart, shippingAddress, cancellationToken);
            var now = DateTime.UtcNow;
            var order = BuildOrder(userId, shippingAddress, request.Notes, pricing, now, null);
            order.Id = await orderRepository.Add(order);
            await cartRepository.ClearCart(cart.Id);

            return ServiceResponse<PlaceOrderResponseDto>.Ok(
                new PlaceOrderResponseDto(order.Id, order.OrderNumber, order.Status, order.TotalAmount),
                "Order placed successfully.",
                201);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResponse<PlaceOrderResponseDto>.Fail(ex.Message, ex.ToString(), 400, "checkout_validation_failed");
        }
        catch (Exception ex)
        {
            return ServiceResponse<PlaceOrderResponseDto>.Fail("Unable to place order.", ex.ToString(), 500, "order_create_failed");
        }
    }

    public async Task<ServiceResponse<IReadOnlyList<OrderDto>>> GetOrdersAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var orders = (await orderRepository.GetByUserId(userId))
                .Select(CheckoutDtoMapper.ToDto)
                .ToList();

            return ServiceResponse<IReadOnlyList<OrderDto>>.Ok(orders);
        }
        catch (Exception ex)
        {
            return ServiceResponse<IReadOnlyList<OrderDto>>.Fail("Unable to fetch orders.", ex.ToString(), 500, "order_read_failed");
        }
    }

    public async Task<ServiceResponse<OrderDto>> GetOrderByIdAsync(string userId, int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await orderRepository.GetById(orderId, userId);
            if (order is null)
            {
                return ServiceResponse<OrderDto>.Fail("Order not found.", statusCode: 404, errorCode: "order_not_found");
            }

            return ServiceResponse<OrderDto>.Ok(CheckoutDtoMapper.ToDto(order));
        }
        catch (Exception ex)
        {
            return ServiceResponse<OrderDto>.Fail("Unable to fetch order.", ex.ToString(), 500, "order_read_failed");
        }
    }

    private static ServiceResponse<ShippingAddressDto> ValidateAddress(
        string fullName,
        string phoneNumber,
        string addressLine1,
        string city,
        string state,
        string postalCode,
        string countryCode)
    {
        if (string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(phoneNumber)
            || string.IsNullOrWhiteSpace(addressLine1)
            || string.IsNullOrWhiteSpace(city)
            || string.IsNullOrWhiteSpace(state)
            || string.IsNullOrWhiteSpace(postalCode)
            || string.IsNullOrWhiteSpace(countryCode))
        {
            return ServiceResponse<ShippingAddressDto>.Fail("All required shipping address fields must be provided.", statusCode: 400, errorCode: "validation_error");
        }

        return ServiceResponse<ShippingAddressDto>.Ok(null!, "Validation passed.");
    }

    private static CheckoutPreviewDto BuildPreview(CheckoutPricingResult pricing, ShippingAddressAggregate shippingAddress)
        => new(
            CheckoutDtoMapper.ToDto(shippingAddress),
            pricing.CurrencyCode,
            pricing.Subtotal,
            pricing.ShippingAmount,
            pricing.ShippingMethod,
            pricing.ShippingLabel,
            pricing.TaxAmount,
            pricing.TaxLabel,
            pricing.TaxRatePercent,
            pricing.TotalAmount,
            pricing.Lines.Select(line => new CheckoutItemDto(
                line.CartItem.Id,
                line.CartItem.ProductDbId,
                line.CartItem.ProductId,
                line.CartItem.Name,
                line.UnitPrice,
                line.CartItem.Quantity,
                line.LineTotal,
                line.Sku,
                line.FulfillmentType,
                line.CartItem.SelectedVariants.Select(variant => new CheckoutSelectedVariantDto(
                    variant.ProductVariantId,
                    variant.ProductVariantOptionId,
                    variant.VariantLabel,
                    variant.OptionValue)).ToList())).ToList());

    private static OrderAggregate BuildOrder(
        string userId,
        ShippingAddressAggregate shippingAddress,
        string? notes,
        CheckoutPricingResult pricing,
        DateTime now,
        string? paymentProvider)
        => new()
        {
            OrderNumber = $"ORD-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            UserId = userId,
            ShippingAddressId = shippingAddress.Id,
            CurrencyCode = pricing.CurrencyCode,
            Status = "PendingPayment",
            PaymentStatus = "Pending",
            PaymentProvider = paymentProvider,
            Subtotal = pricing.Subtotal,
            ShippingAmount = pricing.ShippingAmount,
            ShippingMethod = pricing.ShippingMethod,
            ShippingLabel = pricing.ShippingLabel,
            TaxAmount = pricing.TaxAmount,
            TaxLabel = pricing.TaxLabel,
            TaxRatePercent = pricing.TaxRatePercent,
            TotalAmount = pricing.TotalAmount,
            Notes = notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            ShippingAddress = shippingAddress,
            Items = pricing.Lines.Select(line => new OrderItemAggregate
            {
                ProductDbId = line.CartItem.ProductDbId,
                ProductId = line.Product.ProductId,
                Name = line.Product.Name,
                Slug = line.Product.Slug,
                CoverImageUrl = line.Product.CoverImageUrl,
                CurrencyCode = pricing.CurrencyCode,
                BasePrice = line.BasePrice,
                UnitPrice = line.UnitPrice,
                Quantity = line.CartItem.Quantity,
                LineTotal = line.LineTotal,
                Sku = line.Sku,
                FulfillmentType = line.FulfillmentType,
                SelectedVariants = line.CartItem.SelectedVariants.Select(variant => new OrderItemSelectedVariantAggregate
                {
                    ProductVariantId = variant.ProductVariantId,
                    ProductVariantOptionId = variant.ProductVariantOptionId,
                    VariantLabel = variant.VariantLabel,
                    OptionValue = variant.OptionValue,
                    PriceModifier = variant.PriceModifier,
                    AbsolutePrice = variant.AbsolutePrice,
                    Sku = variant.Sku,
                    FulfillmentType = variant.FulfillmentType?.ToString()
                }).ToList()
            }).ToList()
        };

    private static long ToCurrencySubunits(decimal amount)
        => decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
}
