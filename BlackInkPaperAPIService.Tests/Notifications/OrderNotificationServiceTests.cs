using BlackInkPaperAPIService.Tests.Notifications.Fakes;
using Domain.Aggregates.Ecommerce;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Services;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BlackInkPaperAPIService.Tests.Notifications;

public class OrderNotificationServiceTests
{
    private static readonly Msg91Options Msg91 = new()
    {
        Templates = new Msg91TemplateOptions
        {
            OrderConfirmed = "order_confirmed",
            OrderShipped = "order_shipped",
            OrderDelivered = "order_delivered",
            OrderCancelled = "order_cancelled",
            PaymentFailed = "payment_failed"
        }
    };

    private static OrderNotificationService CreateService(FakeNotificationOutboxRepository outbox)
        => new(outbox, Options.Create(Msg91), NullLogger<OrderNotificationService>.Instance);

    private static OrderAggregate Order(string phone = "9876543210", int id = 1234)
        => new()
        {
            Id = id,
            OrderNumber = $"BIP-{id}",
            UserId = "user-1",
            CurrencyCode = "INR",
            TotalAmount = 15000m,
            ShippingAddress = new ShippingAddressAggregate
            {
                FullName = "Test Customer",
                PhoneNumber = phone
            }
        };

    [Fact]
    public async Task EnqueueAsync_QueuesTheMessage_WhenOrderIsConfirmed()
    {
        var outbox = new FakeNotificationOutboxRepository();

        await CreateService(outbox).EnqueueAsync(Order(), OrderNotificationEvent.Confirmed);

        var queued = Assert.Single(outbox.Enqueued);
        Assert.Equal("order_confirmed", queued.TemplateName);
        Assert.Equal("user-1", queued.UserId);
        Assert.Contains("BIP-1234", queued.Payload);
    }

    /// <summary>
    /// The dedupe key is what stops a customer hearing about the same order twice when both
    /// the client's verify-payment call and Razorpay's webhook report the same capture.
    /// </summary>
    [Fact]
    public async Task EnqueueAsync_UsesAStableDedupeKey_ForTheSameOrderAndEvent()
    {
        var outbox = new FakeNotificationOutboxRepository();
        var service = CreateService(outbox);

        await service.EnqueueAsync(Order(), OrderNotificationEvent.Confirmed);
        await service.EnqueueAsync(Order(), OrderNotificationEvent.Confirmed);

        Assert.Equal(2, outbox.Enqueued.Count);
        Assert.Equal(outbox.Enqueued[0].DedupeKey, outbox.Enqueued[1].DedupeKey);
        Assert.Equal("Confirmed:1234", outbox.Enqueued[0].DedupeKey);
    }

    [Fact]
    public async Task EnqueueAsync_UsesDistinctDedupeKeys_ForDifferentEventsOnOneOrder()
    {
        var outbox = new FakeNotificationOutboxRepository();
        var service = CreateService(outbox);

        await service.EnqueueAsync(Order(), OrderNotificationEvent.Confirmed);
        await service.EnqueueAsync(Order(), OrderNotificationEvent.Shipped);

        Assert.NotEqual(outbox.Enqueued[0].DedupeKey, outbox.Enqueued[1].DedupeKey);
    }

    /// <summary>
    /// Shipping numbers predate phone login and were never normalized, so they arrive in
    /// whatever shape the customer typed.
    /// </summary>
    [Fact]
    public async Task EnqueueAsync_NormalizesTheRecipient_WhenAddressHoldsALocalNumber()
    {
        var outbox = new FakeNotificationOutboxRepository();

        await CreateService(outbox).EnqueueAsync(Order(phone: "09876543210"), OrderNotificationEvent.Shipped);

        Assert.Equal("+919876543210", Assert.Single(outbox.Enqueued).Recipient);
    }

    [Fact]
    public async Task EnqueueAsync_QueuesNothing_WhenShippingPhoneIsUnusable()
    {
        var outbox = new FakeNotificationOutboxRepository();

        await CreateService(outbox).EnqueueAsync(Order(phone: "not a number"), OrderNotificationEvent.Confirmed);

        Assert.Empty(outbox.Enqueued);
    }

    /// <summary>
    /// This runs inside payment capture. A messaging failure must never propagate — an order
    /// that was genuinely paid for cannot be undone because a notification could not be
    /// queued.
    /// </summary>
    [Fact]
    public async Task EnqueueAsync_DoesNotThrow_WhenTheOutboxFails()
    {
        var outbox = new FakeNotificationOutboxRepository
        {
            EnqueueHandler = _ => throw new InvalidOperationException("database is down")
        };

        var exception = await Record.ExceptionAsync(
            () => CreateService(outbox).EnqueueAsync(Order(), OrderNotificationEvent.Confirmed));

        Assert.Null(exception);
    }

    [Fact]
    public async Task EnqueueAsync_DoesNotThrow_WhenTheMessageWasAlreadyQueued()
    {
        var outbox = new FakeNotificationOutboxRepository
        {
            EnqueueHandler = _ => Task.FromResult(false)
        };

        var exception = await Record.ExceptionAsync(
            () => CreateService(outbox).EnqueueAsync(Order(), OrderNotificationEvent.Confirmed));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(OrderNotificationEvent.Confirmed, "order_confirmed")]
    [InlineData(OrderNotificationEvent.Shipped, "order_shipped")]
    [InlineData(OrderNotificationEvent.Delivered, "order_delivered")]
    [InlineData(OrderNotificationEvent.Cancelled, "order_cancelled")]
    [InlineData(OrderNotificationEvent.PaymentFailed, "payment_failed")]
    public async Task EnqueueAsync_SelectsTheConfiguredTemplate_ForEachEvent(
        OrderNotificationEvent notificationEvent, string expectedTemplate)
    {
        var outbox = new FakeNotificationOutboxRepository();

        await CreateService(outbox).EnqueueAsync(Order(), notificationEvent);

        Assert.Equal(expectedTemplate, Assert.Single(outbox.Enqueued).TemplateName);
    }
}
