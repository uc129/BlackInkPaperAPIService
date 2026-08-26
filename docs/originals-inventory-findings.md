# Originals — inventory & sold-out findings

Investigation of whether a purchased **Original** (single, one-of-a-kind piece)
is correctly removed from sale. **Short answer: no.** Two gaps let an Original be
oversold and stay visible after it sells.

## How inventory is applied today

On payment capture the checkout calls
`MarkPaymentCapturedAndApplyInventory` → `ApplyInventoryForOrderItem` per order
item (`OrderRepository.cs:275`, triggered from
`CheckoutApplicationService.cs:238` and `:327`).

`ApplyInventoryForOrderItem` (`OrderRepository.cs:637`):

1. If the item has **physical variants**, decrement each
   `ProductVariantOptions.StockQuantity` under a `StockQuantity >= @Quantity`
   guard (throws on insufficient stock) and return. ✅ Works for Prints.
2. Otherwise, only if **`item.FulfillmentType == physical`**, decrement
   `Products.StockQuantity` (same guard). — `OrderRepository.cs:714-741`
3. Digital / untracked stock (`StockQuantity IS NULL`) is skipped. ✅ Correct.

## Gap 1 — an Original's stock is never decremented

An Original has **no variants**, so `FulfillmentType` is derived from an empty
variant list and is **`null`**:

```
// CartApplicationService.cs:67
var fulfillmentType = selectedVariants
    .LastOrDefault(v => v.FulfillmentType.HasValue)?.FulfillmentType;  // -> null
```

That `null` flows onto the cart item → order item. In
`ApplyInventoryForOrderItem`, the no-variant branch is gated on
`item.FulfillmentType == physical`, so for an Original it **returns without
decrementing** (`OrderRepository.cs:714-717`). `Products.StockQuantity` stays at
`1`, and the oversell guard never runs — **the same Original can be sold more
than once.**

## Gap 2 — sold-out products stay visible

Nothing in checkout/inventory ever sets `IsAvailable = false` (the only writes to
`IsAvailable` are admin create/update). Public visibility filters on
`IsAvailable`, **not** on `StockQuantity`:

- Search: `WHERE (@IsAvailable IS NULL OR p.IsAvailable = @IsAvailable)` with the
  storefront forcing `IsAvailable = true` — stock is not considered
  (`ProductRepository.cs:70`).
- Detail: `GetById`/`GetBySlug` 404 only when `IsAvailable = false`.

So even if stock reached 0, the product would still list and open.

## Scope

This pre-dates the Originals work — it affects **any** no-variant physical
product with tracked stock — but Originals make it acute, since "exactly one
piece" is the entire point.

## Recommended fixes (not yet applied)

1. **Decrement no-variant tracked-stock items.** Gate the product-level decrement
   on `productStock.HasValue` instead of `FulfillmentType == physical`. Digital
   (null stock) is still skipped by the existing `HasValue` guard; the variant
   branch still returns early when it decremented, so there's no double-decrement.
   This makes Originals decrement and lets the `StockQuantity >= @Quantity` guard
   block a second sale.
2. **Hide sold-out.** Either (a) set `IsAvailable = false` when a decrement brings
   `StockQuantity` to 0 (smallest blast radius, public queries unchanged), or
   (b) make public queries treat `StockQuantity = 0` as unavailable
   (`(StockQuantity IS NULL OR StockQuantity > 0)`) — single source of truth, but
   touches search + detail. Recommend (a), optionally both.
3. **Defense in depth (optional).** Re-check stock when adding to cart / placing
   the order so a sold-out Original can't be added (today add-to-cart only checks
   `IsAvailable`, `CartApplicationService.cs:54`).

## Storefront implication

Until fixed, the store cannot rely on the API to remove a sold Original. If you
need correct sold-out behavior before the backend fix lands, the storefront would
have to check `stats.stockQuantity` on the product detail — but the real fix
belongs in checkout/inventory.
