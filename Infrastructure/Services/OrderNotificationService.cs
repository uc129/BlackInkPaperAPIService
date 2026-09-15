using System.Globalization;
using System.Text.Json;
using Domain.Aggregates.Ecommerce;
using Domain.Entities;
using Infrastructure.Configuration;
using Infrastructure.Contracts.Repositories;
using Infrastructure.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Turns an order event into a queued WhatsApp message.
///
/// Nothing is sent from here. The row goes into the outbox and the dispatcher delivers it,
/// so a slow provider cannot hold up a checkout response and an outage cannot lose the
/// message.
/// </summary>
public sealed class OrderNotificationService(
    INotificationOutboxRepository outbox,
    IOptions<Msg91Options> optionsAccessor,
    ILogger<OrderNotificationService> logger) : IOrderNotificationService
{
    private const string WhatsAppChannel = "WhatsApp";
    private const int MaxAttempts = 5;

    private static readonly CultureInfo IndianCulture = new("en-IN");

    private readonly Msg91Options options = optionsAccessor.Value;

    public async Task EnqueueAsync(
        OrderAggregate order, OrderNotificationEvent notificationEvent, CancellationToken ct = default)
    {
        try
        {
            var template = ResolveTemplate(notificationEvent);
            if (string.IsNullOrWhiteSpace(template))
            {
                logger.LogWarning("No template configured for {Event}; nothing queued.", notificationEvent);
                return;
            }

            // Shipping phone numbers were captured long before any of this existed and were
            // never normalized, so they can be anything the customer typed.
            var recipient = PhoneNumberNormalizer.Normalize(order.ShippingAddress.PhoneNumber);
            if (!recipient.Success)
            {
                logger.LogWarning(
                    "Order {OrderNumber} has an unusable shipping phone number; {Event} not queued.",
                    order.OrderNumber, notificationEvent);
                return;
            }

            var now = DateTime.UtcNow;
            var queued = await outbox.EnqueueAsync(new NotificationDelivery
            {
                DedupeKey = $"{notificationEvent}:{order.Id}",
                UserId = order.UserId,
                Recipient = recipient.Value,
                Channel = WhatsAppChannel,
                TemplateName = template,
                Payload = JsonSerializer.Serialize(new { parameters = BuildParameters(order) }),
                Status = NotificationDeliveryStatus.Pending,
                Attempts = 0,
                MaxAttempts = MaxAttempts,
                ScheduledFor = now,
                CreatedAt = now,
                UpdatedAt = now
            }, ct);

            if (!queued)
            {
                // Expected, not exceptional: a captured payment reaches us from both the
                // client and the webhook.
                logger.LogDebug(
                    "{Event} for order {OrderNumber} was already queued; skipped.",
                    notificationEvent, order.OrderNumber);
            }
        }
        catch (Exception ex)
        {
            // Deliberately swallowed. The caller is mid-payment-capture or mid-status-change;
            // failing to tell someone about an order must not undo the order.
            logger.LogError(ex,
                "Failed to queue {Event} for order {OrderNumber}.",
                notificationEvent, order.OrderNumber);
        }
    }

    private string ResolveTemplate(OrderNotificationEvent notificationEvent) => notificationEvent switch
    {
        OrderNotificationEvent.Confirmed => options.Templates.OrderConfirmed,
        OrderNotificationEvent.PaymentFailed => options.Templates.PaymentFailed,
        OrderNotificationEvent.Shipped => options.Templates.OrderShipped,
        OrderNotificationEvent.Delivered => options.Templates.OrderDelivered,
        OrderNotificationEvent.Cancelled => options.Templates.OrderCancelled,
        _ => string.Empty
    };

    /// <summary>
    /// Positional template variables, in the order the approved Meta templates declare them:
    /// customer name, order number, order total. Changing this order silently reshuffles what
    /// customers read, so it must be kept in step with the approved template bodies.
    /// </summary>
    private static string[] BuildParameters(OrderAggregate order) =>
    [
        order.ShippingAddress.FullName,
        order.OrderNumber,
        FormatAmount(order.TotalAmount, order.CurrencyCode)
    ];

    private static string FormatAmount(decimal amount, string currencyCode)
        => string.Equals(currencyCode, "INR", StringComparison.OrdinalIgnoreCase)
            ? amount.ToString("C0", IndianCulture)
            : $"{currencyCode} {amount:N2}";
}
