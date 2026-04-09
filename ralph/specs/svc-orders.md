# Order Services

## Bounded Context
Order processing — order placement workflow, shopping cart, checkout attributes, order totals calculation, gift cards, return requests, and recurring payments.

## Legacy Source
- `src/Libraries/Nop.Services/Orders/` — all files
- Key interfaces: `IOrderService`, `IOrderProcessingService` (3167 LOC), `IShoppingCartService`, `ICheckoutAttributeService`, `ICheckoutAttributeParser`, `ICheckoutAttributeFormatter`, `IGiftCardService`, `IOrderTotalCalculationService`, `IReturnRequestService`, `IOrderReportService`, `IRewardPointService`, `ICustomNumberFormatter`
- Domain: `Nop.Core.Domain.Orders`

## Key Entities
- Order, OrderItem, OrderNote
- ShoppingCartItem (cart + wishlist)
- CheckoutAttribute, CheckoutAttributeValue
- GiftCard, GiftCardUsageHistory
- ReturnRequest, ReturnRequestAction, ReturnRequestReason
- RecurringPayment, RecurringPaymentHistory

## External Dependencies
- Depends on: Payment services, Shipping services, Tax services, Discount services, Catalog services

## Migration Notes
- **Decision**: Rewrite
- **Complexity**: CRITICAL — `OrderProcessingService.PlaceOrder` is the most complex method in the system (3167 LOC total service)
- Order placement workflow: validation → payment processing → order creation → inventory reduction → cart clearing → notifications → events
- Order state machine: Pending → Processing → Complete / Cancelled; Payment: Pending → Authorized → Paid → Refunded/Voided
- Price snapshots: order items freeze pricing at order time
- Gift card lifecycle: purchase → activation on order complete → redemption → partial balance tracking
- Recurring payments: cycle-based scheduling with auto-cancel on failure threshold

## Acceptance Criteria
- [ ] `PlaceOrder` executes the full workflow: validate → process payment → create order → reduce inventory → clear cart → send notifications
- [ ] Order status transitions follow the defined state machine (no invalid transitions)
- [ ] `OrderTotalCalculationService` produces correct subtotal, tax, shipping, discount, and total amounts
- [ ] Shopping cart operations (add, update, remove, migrate guest→registered) work correctly
- [ ] Gift card purchase creates card on order completion; redemption deducts from balance correctly
