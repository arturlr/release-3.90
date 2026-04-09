# nopCommerce Decision Logic Documentation

## Overview

This document captures the decision trees, conditional logic patterns, and business rule evaluation mechanisms throughout the nopCommerce platform. These represent the key decision points that control system behavior.

## Table of Contents
- [Product Availability Decision Tree](#product-availability-decision-tree)
- [Pricing Decision Logic](#pricing-decision-logic)
- [Discount Applicability Decision Tree](#discount-applicability-decision-tree)
- [Shipping Method Selection Logic](#shipping-method-selection-logic)
- [Payment Authorization Decision](#payment-authorization-decision)
- [Order Status Transition Logic](#order-status-transition-logic)
- [Tax Calculation Decision Tree](#tax-calculation-decision-tree)
- [Access Control Decision Logic](#access-control-decision-logic)

---

## Product Availability Decision Tree

**Question**: Is this product available for purchase by this customer?

```
START: Check Product Availability
│
├─> Is Product.Deleted = true?
│   └─> YES → UNAVAILABLE (product removed)
│   └─> NO → Continue
│
├─> Is Product.Published = false?
│   └─> YES → Is CurrentUser.IsAdmin?
│   │   └─> YES → AVAILABLE (admin preview)
│   │   └─> NO → UNAVAILABLE (not published to public)
│   └─> NO → Continue
│
├─> Is AvailableStartDateTimeUtc set AND current date < start date?
│   └─> YES → UNAVAILABLE (not yet available)
│   └─> NO → Continue
│
├─> Is AvailableEndDateTimeUtc set AND current date > end date?
│   └─> YES → UNAVAILABLE (availability expired)
│   └─> NO → Continue
│
├─> Is Product.SubjectToAcl = true?
│   └─> YES → Is customer's role in Product.AllowedCustomerRoles?
│   │   └─> YES → Continue
│   │   └─> NO → UNAVAILABLE (access restricted)
│   └─> NO → Continue
│
├─> Is Product.LimitedToStores = true?
│   └─> YES → Is current store in Product.StoreMappings?
│   │   └─> YES → Continue
│   │   └─> NO → UNAVAILABLE (not available in this store)
│   └─> NO → Continue
│
├─> Is Product.DisableBuyButton = true?
│   └─> YES → UNAVAILABLE (buy button disabled)
│   └─> NO → Continue
│
├─> Is Product.CallForPrice = true?
│   └─> YES → AVAILABLE (but must contact for price)
│   └─> NO → Continue
│
├─> Is Product.AvailableForPreOrder = true?
│   └─> YES → AVAILABLE (can preorder)
│   └─> NO → Check inventory
│
└─> INVENTORY CHECK:
    │
    ├─> ManageInventoryMethod = DontManageStock?
    │   └─> YES → AVAILABLE (unlimited stock)
    │   └─> NO → Continue
    │
    ├─> ManageInventoryMethod = ManageStock?
    │   └─> YES → Is (StockQuantity - MinStockQuantity) >= RequestedQuantity?
    │       ├─> YES → AVAILABLE (sufficient stock)
    │       └─> NO → Is AllowBackInStockSubscriptions = true?
    │           ├─> YES → UNAVAILABLE (but can subscribe)
    │           └─> NO → UNAVAILABLE (out of stock)
    │
    └─> ManageInventoryMethod = ManageStockByAttributes?
        └─> YES → Check attribute combination stock
            └─> For selected combination:
                └─> Is combination.StockQuantity >= RequestedQuantity?
                    ├─> YES → AVAILABLE
                    └─> NO → UNAVAILABLE

RESULT: AVAILABLE or UNAVAILABLE (with specific reason)
```

**Location**: `Nop.Services.Catalog.ProductService.SearchProducts()` and availability checks throughout

---

## Pricing Decision Logic

**Question**: What is the final price for this product for this customer?

```
START: Calculate Final Price for Product
│
├─> INPUT:
│   ├─> Product
│   ├─> Customer
│   ├─> Quantity
│   ├─> SelectedAttributes
│   └─> IncludeDiscounts flag
│
├─> STEP 1: Determine Base Price
│   │
│   ├─> Is Product.CustomerEntersPrice = true?
│   │   └─> YES → Use customer-entered price → Skip to STEP 6
│   │   └─> NO → Continue
│   │
│   ├─> Is Product.CallForPrice = true?
│   │   └─> YES → Return 0 (no price displayed)
│   │   └─> NO → Continue
│   │
│   └─> BasePrice = Product.Price
│
├─> STEP 2: Check Tier Pricing
│   │
│   ├─> Get all TierPrices for Product
│   ├─> Filter by:
│   │   ├─> Quantity <= requested quantity
│   │   ├─> StoreId matches current store (or 0 for all stores)
│   │   └─> CustomerRoleId matches customer's roles (or null for all)
│   │
│   ├─> Are there applicable tier prices?
│   │   └─> YES → Select tier price with highest quantity threshold
│   │       └─> BasePrice = TierPrice.Price
│   │   └─> NO → Keep Product.Price
│   │
│   └─> Continue
│
├─> STEP 3: Apply Product Attribute Price Adjustments
│   │
│   ├─> Parse SelectedAttributes XML
│   ├─> For each selected attribute value:
│   │   ├─> Get ProductAttributeValue.PriceAdjustment
│   │   ├─> Is PriceAdjustmentUsePercentage = true?
│   │   │   └─> YES → Adjustment = BasePrice × (PriceAdjustment / 100)
│   │   │   └─> NO → Adjustment = PriceAdjustment
│   │   └─> BasePrice += Adjustment
│   │
│   └─> Continue
│
├─> STEP 4: Check Product Attribute Combinations
│   │
│   ├─> Is there a combination matching SelectedAttributes?
│   │   └─> YES → Is combination.OverriddenPrice set?
│   │       └─> YES → BasePrice = combination.OverriddenPrice
│   │       └─> NO → Keep current price
│   │   └─> NO → Keep current price
│   │
│   └─> Continue
│
├─> STEP 5: Apply Discounts (if IncludeDiscounts = true)
│   │
│   ├─> Get all active discounts
│   ├─> Filter applicable discounts:
│   │   │
│   │   ├─> Is Discount.IsActive = false? → Exclude
│   │   ├─> Is current date outside [StartDateUtc, EndDateUtc]? → Exclude
│   │   ├─> Is Discount.RequiresCouponCode = true?
│   │   │   └─> Is valid coupon code entered? → No → Exclude
│   │   ├─> Has Discount.LimitationTimes been reached? → Exclude
│   │   ├─> Has customer exceeded per-customer limit? → Exclude
│   │   │
│   │   └─> For each discount requirement:
│   │       ├─> Type = MustBeAssignedToCustomerRole?
│   │       │   └─> Is customer in required role? → No → Exclude
│   │       ├─> Type = HadSpentAmount?
│   │       │   └─> Has customer spent >= amount? → No → Exclude
│   │       └─> Type = HasOneProduct?
│   │           └─> Is required product in cart? → No → Exclude
│   │
│   ├─> Determine discount type:
│   │   │
│   │   ├─> AssignedToSkus (product-specific)?
│   │   │   └─> Does discount apply to this product SKU?
│   │   │       └─> YES → Applicable
│   │   │
│   │   ├─> AssignedToCategories?
│   │   │   └─> Is product in discounted category?
│   │   │       └─> YES → Applicable
│   │   │
│   │   └─> AssignedToManufacturers?
│   │       └─> Is product from discounted manufacturer?
│   │           └─> YES → Applicable
│   │
│   ├─> Sort applicable discounts by:
│   │   ├─> UsePercentage? → Percentage discounts first
│   │   └─> Then by discount amount (highest first)
│   │
│   ├─> Apply best discount:
│   │   ├─> If UsePercentage = true:
│   │   │   └─> Discount = BasePrice × (DiscountPercentage / 100)
│   │   ├─> If UsePercentage = false:
│   │   │   └─> Discount = DiscountAmount
│   │   │
│   │   ├─> Is MaximumDiscountAmount set?
│   │   │   └─> YES → Discount = MIN(Discount, MaximumDiscountAmount)
│   │   │
│   │   └─> FinalPrice = BasePrice - Discount
│   │
│   └─> Continue
│
├─> STEP 6: Apply Currency Conversion
│   │
│   ├─> Get customer's WorkingCurrency
│   ├─> Is WorkingCurrency different from PrimaryCurrency?
│   │   └─> YES → FinalPrice = FinalPrice × ExchangeRate
│   │   └─> NO → No conversion needed
│   │
│   └─> Continue
│
├─> STEP 7: Apply Price Rounding
│   │
│   ├─> Get RoundingType from settings:
│   │   ├─> Rounding001 → Round to 2 decimals (0.01)
│   │   ├─> Rounding005 → Round to nearest 0.05
│   │   ├─> Rounding01 → Round to nearest 0.10
│   │   ├─> Rounding05 → Round to nearest 0.50
│   │   └─> Rounding1 → Round to nearest 1.00
│   │
│   └─> FinalPrice = Round(FinalPrice, RoundingType)
│
└─> RETURN: FinalPrice

```

**Location**: `Nop.Services.Catalog.PriceCalculationService.GetFinalPrice()`

---

## Discount Applicability Decision Tree

**Question**: Can this discount be applied to this order/product?

```
START: Validate Discount
│
├─> Is Discount.Deleted = true?
│   └─> YES → NOT APPLICABLE
│   └─> NO → Continue
│
├─> Is Discount.IsActive = false?
│   └─> YES → NOT APPLICABLE
│   └─> NO → Continue
│
├─> Is StartDateUtc set AND current date < StartDateUtc?
│   └─> YES → NOT APPLICABLE (not started yet)
│   └─> NO → Continue
│
├─> Is EndDateUtc set AND current date > EndDateUtc?
│   └─> YES → NOT APPLICABLE (expired)
│   └─> NO → Continue
│
├─> Is RequiresCouponCode = true?
│   └─> YES → Is valid coupon code provided?
│   │   └─> YES → Continue
│   │   └─> NO → NOT APPLICABLE (coupon required)
│   └─> NO → Continue
│
├─> USAGE LIMITS CHECK:
│   │
│   ├─> Is LimitationTimes > 0?
│   │   └─> YES → Is DiscountUsageHistory.Count >= LimitationTimes?
│   │       └─> YES → NOT APPLICABLE (usage limit reached)
│   │       └─> NO → Continue
│   │
│   └─> Is PerCustomerLimitation > 0?
│       └─> YES → Usage by this customer >= PerCustomerLimitation?
│           └─> YES → NOT APPLICABLE (customer limit reached)
│           └─> NO → Continue
│
├─> DISCOUNT REQUIREMENTS CHECK:
│   │
│   ├─> Get all DiscountRequirements for this discount
│   │
│   ├─> For each requirement:
│   │   │
│   │   ├─> RequirementType = CustomerRole?
│   │   │   └─> Is customer in RestrictedToCustomerRoleId?
│   │   │       └─> NO → NOT APPLICABLE
│   │   │
│   │   ├─> RequirementType = HadSpentAmount?
│   │   │   └─> Calculate customer's total spent
│   │   │       └─> Total < SpentAmount? → NOT APPLICABLE
│   │   │
│   │   ├─> RequirementType = HasOneProduct?
│   │   │   └─> Is RestrictedProductId in shopping cart?
│   │   │       └─> NO → NOT APPLICABLE
│   │   │
│   │   ├─> RequirementType = HasAllProducts?
│   │   │   └─> Are all RestrictedProductIds in cart?
│   │   │       └─> NO → NOT APPLICABLE
│   │   │
│   │   └─> [Plugin-defined requirements evaluated]
│   │
│   └─> All requirements met? → Continue
│
├─> DISCOUNT TYPE SPECIFIC CHECK:
│   │
│   ├─> DiscountType = AssignedToOrderTotal?
│   │   └─> YES → Check if order subtotal >= minimum (if set)
│   │       └─> YES → APPLICABLE
│   │       └─> NO → NOT APPLICABLE
│   │
│   ├─> DiscountType = AssignedToSkus?
│   │   └─> YES → Is product SKU in discount's AppliedToProducts?
│   │       └─> YES → APPLICABLE
│   │       └─> NO → NOT APPLICABLE
│   │
│   ├─> DiscountType = AssignedToCategories?
│   │   └─> YES → Is product in any of discount's AppliedToCategories?
│   │       └─> YES → APPLICABLE
│   │       └─> NO → NOT APPLICABLE
│   │
│   ├─> DiscountType = AssignedToManufacturers?
│   │   └─> YES → Is product from discount's AppliedToManufacturers?
│   │       └─> YES → APPLICABLE
│   │       └─> NO → NOT APPLICABLE
│   │
│   └─> DiscountType = AssignedToShipping?
│       └─> YES → Is shipping method selected?
│           └─> YES → APPLICABLE
│           └─> NO → NOT APPLICABLE
│
└─> RESULT: APPLICABLE or NOT APPLICABLE (with reason)
```

**Location**: `Nop.Services.Discounts.DiscountService.ValidateDiscount()`

---

## Shipping Method Selection Logic

**Question**: Which shipping methods are available for this order?

```
START: Get Shipping Options
│
├─> INPUT:
│   ├─> ShoppingCart
│   ├─> ShippingAddress
│   ├─> Customer
│   └─> StoreId
│
├─> PRE-CHECKS:
│   │
│   ├─> Does order require shipping?
│   │   └─> Check all cart items: Any with RequiresShipping = true?
│   │       └─> NO → Return "No Shipping Required"
│   │       └─> YES → Continue
│   │
│   ├─> Is ShippingAddress provided?
│   │   └─> NO → Error: "Shipping address required"
│   │   └─> YES → Continue
│   │
│   └─> Continue
│
├─> STEP 1: Get Active Shipping Rate Computation Methods
│   │
│   ├─> Load all IShippingRateComputationMethod plugins
│   ├─> Filter:
│   │   ├─> Plugin.IsActive = true?
│   │   ├─> Available in current store?
│   │   └─> No country restrictions OR ShippingAddress.Country allowed?
│   │
│   └─> ShippingMethods = []
│
├─> STEP 2: For Each Shipping Rate Computation Method
│   │
│   ├─> Call method.GetShippingOptions(request)
│   │   │
│   │   ├─> Method evaluates:
│   │   │   ├─> Weight restrictions
│   │   │   ├─> Dimension restrictions
│   │   │   ├─> Destination country/region
│   │   │   ├─> Order value minimums
│   │   │   └─> [Method-specific logic]
│   │   │
│   │   └─> Returns list of ShippingOption objects
│   │
│   ├─> For each returned ShippingOption:
│   │   │
│   │   ├─> VALIDATION CHECKS:
│   │   │   │
│   │   │   ├─> Is ShippingMethod.Published = false?
│   │   │   │   └─> YES → Skip this option
│   │   │   │
│   │   │   ├─> Is ShippingMethod.LimitedToStores = true?
│   │   │   │   └─> YES → Is current store in allowed stores?
│   │   │   │       └─> NO → Skip this option
│   │   │   │
│   │   │   ├─> Country Restrictions:
│   │   │   │   └─> Are there RestrictedCountries?
│   │   │   │       └─> YES → Is shipping country in restricted list?
│   │   │   │           └─> YES → Skip this option
│   │   │   │
│   │   │   ├─> Customer Role Restrictions:
│   │   │   │   └─> Are there RestrictedRoles?
│   │   │   │       └─> YES → Is customer in allowed roles?
│   │   │   │           └─> NO → Skip this option
│   │   │   │
│   │   │   └─> Weight/Dimension Check:
│   │   │       ├─> Calculate total cart weight
│   │   │       ├─> If MaxWeight > 0 AND cart weight > MaxWeight
│   │   │       │   └─> Skip this option
│   │   │       └─> [Similar for dimensions]
│   │   │
│   │   ├─> RATE CALCULATION:
│   │   │   │
│   │   │   ├─> BaseRate = method calculated rate
│   │   │   │
│   │   │   ├─> Apply Additional Charge:
│   │   │   │   └─> Rate += ShippingMethod.AdditionalCharge
│   │   │   │
│   │   │   ├─> Apply Markup:
│   │   │   │   ├─> If MarkupPercentage > 0:
│   │   │   │   │   └─> Rate += Rate × (MarkupPercentage / 100)
│   │   │   │   └─> If MarkupAmount > 0:
│   │   │   │       └─> Rate += MarkupAmount
│   │   │   │
│   │   │   └─> Check for Free Shipping:
│   │   │       │
│   │   │       ├─> Is any cart item marked FreeShipping?
│   │   │       │   └─> YES → Rate = 0
│   │   │       │
│   │   │       ├─> Does customer have free shipping discount?
│   │   │       │   └─> YES → Rate = 0
│   │   │       │
│   │   │       └─> Is order total >= FreeShippingOverXValue?
│   │   │           └─> YES → Rate = 0
│   │   │
│   │   └─> Add to ShippingMethods list
│   │
│   └─> Continue to next method
│
├─> STEP 3: Sort Shipping Options
│   │
│   ├─> Primary sort: By DisplayOrder
│   └─> Secondary sort: By Rate (ascending)
│
├─> STEP 4: Check if empty
│   │
│   ├─> ShippingMethods.Count = 0?
│   │   └─> YES → Return error "No shipping options available"
│   │   └─> NO → Continue
│   │
│   └─> Continue
│
└─> RETURN: List of available ShippingOption objects
    └─> Each contains:
        ├─> Name
        ├─> Description
        ├─> Rate
        ├─> ShippingRateComputationMethodSystemName
        └─> Transit days (optional)
```

**Location**: `Nop.Services.Shipping.ShippingService.GetShippingOptions()`

---

## Payment Authorization Decision

**Question**: Should this payment be authorized?

```
START: Payment Authorization Check
│
├─> STEP 1: Payment Method Validation
│   │
│   ├─> Is payment method active?
│   │   └─> NO → DECLINE (payment method disabled)
│   │   └─> YES → Continue
│   │
│   ├─> Is payment method available in customer's country?
│   │   └─> NO → DECLINE (geographic restriction)
│   │   └─> YES → Continue
│   │
│   ├─> Is customer's role allowed for this payment method?
│   │   └─> NO → DECLINE (role restriction)
│   │   └─> YES → Continue
│   │
│   └─> Continue
│
├─> STEP 2: Order Amount Validation
│   │
│   ├─> Is order total <= 0?
│   │   └─> YES → DECLINE (invalid amount)
│   │   └─> NO → Continue
│   │
│   ├─> Does payment method have minimum order total?
│   │   └─> YES → Is order total >= minimum?
│   │       └─> NO → DECLINE (below minimum)
│   │       └─> YES → Continue
│   │
│   ├─> Does payment method have maximum order total?
│   │   └─> YES → Is order total <= maximum?
│   │       └─> NO → DECLINE (above maximum)
│   │       └─> YES → Continue
│   │
│   └─> Continue
│
├─> STEP 3: Payment Information Validation
│   │
│   ├─> PaymentMethodType = Standard (credit card)?
│   │   └─> YES → Validate credit card data:
│   │       ├─> Card number valid format?
│   │       ├─> Expiration date in future?
│   │       ├─> CVV provided (if required)?
│   │       ├─> Cardholder name provided?
│   │       └─> All valid → Continue
│   │           └─> Any invalid → DECLINE (invalid payment info)
│   │
│   ├─> PaymentMethodType = Redirection?
│   │   └─> YES → No validation needed at this stage
│   │       └─> Will validate at external gateway
│   │
│   └─> Continue
│
├─> STEP 4: Fraud Detection Checks
│   │
│   ├─> Is billing address provided?
│   │   └─> NO → DECLINE (address required)
│   │   └─> YES → Continue
│   │
│   ├─> Does billing address match card address (if checked)?
│   │   └─> NO → DECLINE (address mismatch)
│   │   └─> YES → Continue
│   │
│   ├─> Is customer email verified?
│   │   └─> NO → DECLINE (unverified email)
│   │   └─> YES → Continue
│   │
│   ├─> Check for suspicious patterns:
│   │   ├─> Multiple orders in short time?
│   │   ├─> Different cards from same IP?
│   │   ├─> High-value order from new customer?
│   │   └─> Shipping to high-risk country?
│   │       └─> Any red flags → DECLINE or MANUAL_REVIEW
│   │
│   └─> Continue
│
├─> STEP 5: Inventory Verification
│   │
│   ├─> For each cart item:
│   │   ├─> Is product still available?
│   │   ├─> Is sufficient stock available?
│   │   └─> Has price changed significantly?
│   │       └─> Any issues → DECLINE (cart changed)
│   │
│   └─> Continue
│
├─> STEP 6: Gateway Authorization
│   │
│   ├─> Build authorization request:
│   │   ├─> Amount
│   │   ├─> Currency
│   │   ├─> Card/account details
│   │   ├─> Billing address
│   │   └─> Order reference
│   │
│   ├─> Call payment gateway API
│   │
│   ├─> Gateway Response Evaluation:
│   │   │
│   │   ├─> Response = "Approved"?
│   │   │   └─> AUTHORIZE (store transaction ID)
│   │   │
│   │   ├─> Response = "Declined"?
│   │   │   └─> DECLINE (reason: insufficient funds, card declined, etc.)
│   │   │
│   │   ├─> Response = "Pending"?
│   │   │   └─> PENDING (await async confirmation)
│   │   │
│   │   ├─> Response = "Fraud Review"?
│   │   │   └─> MANUAL_REVIEW (hold order)
│   │   │
│   │   └─> Response = "Error"?
│   │       └─> DECLINE (technical error)
│   │
│   └─> Store gateway response details
│
└─> DECISION RESULT:
    ├─> AUTHORIZE → Create order, mark as Authorized
    ├─> DECLINE → Show error, don't create order
    ├─> PENDING → Create order, mark as Pending, await callback
    └─> MANUAL_REVIEW → Create order, flag for admin review
```

**Location**: 
- `Nop.Services.Payments.PaymentService.ProcessPayment()`
- Payment gateway plugins

---

## Order Status Transition Logic

**Question**: Can the order status transition from current state to requested state?

```
START: Order Status Transition Request
│
├─> INPUT:
│   ├─> CurrentOrderStatus
│   ├─> CurrentPaymentStatus
│   ├─> CurrentShippingStatus
│   └─> RequestedAction
│
├─> ACTION: Cancel Order
│   │
│   ├─> Current OrderStatus = Cancelled?
│   │   └─> YES → INVALID (already cancelled)
│   │   └─> NO → Continue
│   │
│   ├─> Current OrderStatus = Complete?
│   │   └─> YES → INVALID (completed orders cannot be cancelled)
│   │   └─> NO → Continue
│   │
│   ├─> Current PaymentStatus = Refunded?
│   │   └─> YES → INVALID (already refunded)
│   │   └─> NO → Continue
│   │
│   ├─> Current ShippingStatus = Delivered?
│   │   └─> YES → INVALID (already delivered, use return instead)
│   │   └─> NO → ALLOW cancellation
│   │       └─> Set OrderStatus = Cancelled
│   │       └─> Restore inventory
│   │       └─> Cancel recurring payments
│   │
│   └─> TRANSITION: → Cancelled
│
├─> ACTION: Mark as Paid
│   │
│   ├─> Current PaymentStatus = Paid?
│   │   └─> YES → INVALID (already paid)
│   │   └─> NO → Continue
│   │
│   ├─> Current PaymentStatus = Refunded?
│   │   └─> YES → INVALID (refunded orders cannot be marked paid)
│   │   └─> NO → Continue
│   │
│   ├─> Current OrderStatus = Cancelled?
│   │   └─> YES → INVALID (cancelled orders cannot be paid)
│   │   └─> NO → ALLOW marking as paid
│   │       └─> Set PaymentStatus = Paid
│   │       └─> If OrderStatus = Pending, change to Processing
│   │
│   └─> TRANSITION: PaymentStatus → Paid
│
├─> ACTION: Ship Order
│   │
│   ├─> Current ShippingStatus = Delivered?
│   │   └─> YES → INVALID (already delivered)
│   │   └─> NO → Continue
│   │
│   ├─> Current OrderStatus = Cancelled?
│   │   └─> YES → INVALID (cancelled orders cannot ship)
│   │   └─> NO → Continue
│   │
│   ├─> Current PaymentStatus in [Pending, Authorized]?
│   │   └─> YES → WARNING (ship without payment?)
│   │       └─> Require admin confirmation
│   │   └─> NO → Continue
│   │
│   ├─> Create shipment
│   ├─> Set ShippingStatus = Shipped (or PartiallyShipped)
│   ├─> If OrderStatus = Pending, change to Processing
│   │
│   └─> TRANSITION: ShippingStatus → Shipped
│
├─> ACTION: Mark as Delivered
│   │
│   ├─> Current ShippingStatus != Shipped?
│   │   └─> YES → INVALID (must be shipped first)
│   │   └─> NO → Continue
│   │
│   ├─> ALLOW marking as delivered
│   │   └─> Set ShippingStatus = Delivered
│   │   └─> Set DeliveredDateUtc
│   │   └─> If all shipments delivered, consider completing order
│   │
│   └─> TRANSITION: ShippingStatus → Delivered
│
├─> ACTION: Complete Order
│   │
│   ├─> Current OrderStatus = Complete?
│   │   └─> YES → INVALID (already complete)
│   │   └─> NO → Continue
│   │
│   ├─> Current PaymentStatus = Paid?
│   │   └─> NO → INVALID (payment must be complete)
│   │   └─> YES → Continue
│   │
│   ├─> If requires shipping:
│   │   └─> Current ShippingStatus = Delivered?
│   │       └─> NO → INVALID (must be delivered first)
│   │       └─> YES → Continue
│   │
│   ├─> ALLOW completion
│   │   └─> Set OrderStatus = Complete
│   │   └─> Award reward points
│   │   └─> Publish OrderCompletedEvent
│   │
│   └─> TRANSITION: OrderStatus → Complete
│
├─> ACTION: Refund Order
│   │
│   ├─> Current PaymentStatus != Paid?
│   │   └─> YES → INVALID (only paid orders can be refunded)
│   │   └─> NO → Continue
│   │
│   ├─> Payment method supports refunds?
│   │   └─> NO → Use offline refund instead
│   │   └─> YES → Continue
│   │
│   ├─> Process refund through gateway
│   │   ├─> Success → Set PaymentStatus = Refunded
│   │   └─> Failure → Show error, keep current status
│   │
│   ├─> Optionally restore inventory
│   │
│   └─> TRANSITION: PaymentStatus → Refunded (or PartiallyRefunded)
│
└─> INVALID transitions return error message explaining why
```

**Location**: `Nop.Services.Orders.OrderProcessingService` (various methods)

---

## Tax Calculation Decision Tree

**Question**: What tax rate applies to this line item?

```
START: Determine Tax Rate
│
├─> INPUT:
│   ├─> Product
│   ├─> Customer
│   ├─> ShippingAddress (or BillingAddress)
│   └─> StoreId
│
├─> STEP 1: Check Tax Exemptions
│   │
│   ├─> Is Customer.IsTaxExempt = true?
│   │   └─> YES → Tax Rate = 0%, DONE
│   │   └─> NO → Continue
│   │
│   ├─> Is customer in tax-exempt role?
│   │   └─> YES → Tax Rate = 0%, DONE
│   │   └─> NO → Continue
│   │
│   ├─> Is Product.IsTaxExempt = true?
│   │   └─> YES → Tax Rate = 0%, DONE
│   │   └─> NO → Continue
│   │
│   └─> Continue
│
├─> STEP 2: Determine Tax Address
│   │
│   ├─> Check TaxSettings.TaxBasedOn:
│   │   ├─> BillingAddress → Use BillingAddress
│   │   ├─> ShippingAddress → Use ShippingAddress
│   │   └─> DefaultAddress → Use store default address
│   │
│   ├─> Is address null or incomplete?
│   │   └─> YES → Use default store address
│   │   └─> NO → Use determined address
│   │
│   └─> TaxAddress = selected address
│
├─> STEP 3: Get Product Tax Category
│   │
│   ├─> TaxCategoryId = Product.TaxCategoryId
│   │
│   ├─> Is TaxCategoryId = 0 or null?
│   │   └─> YES → Use default tax category from settings
│   │   └─> NO → Use product's tax category
│   │
│   └─> Continue
│
├─> STEP 4: Call Tax Provider
│   │
│   ├─> Create CalculateTaxRequest:
│   │   ├─> TaxCategoryId
│   │   ├─> Address (country, state, zip)
│   │   ├─> Customer
│   │   └─> Product price
│   │
│   ├─> Get active ITaxProvider plugin
│   │
│   ├─> Call taxProvider.GetTaxRate(request)
│   │   │
│   │   ├─> Provider Logic (typical):
│   │   │   │
│   │   │   ├─> Query TaxRate table:
│   │   │   │   ├─> WHERE TaxCategoryId = request.TaxCategoryId
│   │   │   │   ├─> AND CountryId = address.CountryId
│   │   │   │   ├─> AND (StateProvinceId = address.StateId OR StateProvinceId IS NULL)
│   │   │   │   ├─> AND (Zip matches OR Zip IS NULL)
│   │   │   │   └─> ORDER BY specificity (most specific first)
│   │   │   │
│   │   │   ├─> Multiple matching rates?
│   │   │   │   ├─> Prioritize most specific match:
│   │   │   │   │   ├─> Country + State + Zip (highest priority)
│   │   │   │   │   ├─> Country + State
│   │   │   │   │   ├─> Country + Zip
│   │   │   │   │   └─> Country only (lowest priority)
│   │   │   │   └─> Select highest priority rate
│   │   │   │
│   │   │   ├─> No matching rate found?
│   │   │   │   └─> Return default rate (0% or configured default)
│   │   │   │
│   │   │   └─> Return tax rate percentage
│   │   │
│   │   └─> Provider returns CalculateTaxResult
│   │
│   └─> TaxRate = result.TaxRate
│
├─> STEP 5: Handle Multiple Tax Jurisdictions
│   │
│   ├─> Are there multiple applicable tax rates? (e.g., state + local)
│   │   └─> YES → Sum all applicable rates
│   │       └─> TotalTaxRate = Rate1 + Rate2 + ...
│   │   └─> NO → TotalTaxRate = single rate
│   │
│   └─> Continue
│
├─> STEP 6: Calculate Tax Amount
│   │
│   ├─> TaxAmount = ProductPrice × (TotalTaxRate / 100)
│   │
│   ├─> Round tax amount according to settings
│   │
│   └─> Store both:
│       ├─> PriceExcludingTax = ProductPrice
│       └─> PriceIncludingTax = ProductPrice + TaxAmount
│
└─> RETURN: TaxRate, TaxAmount, PriceInclTax, PriceExclTax
```

**Special Cases**:
- **Shipping Tax**: Separate tax calculation for shipping charges if `TaxSettings.ShippingIsTaxable = true`
- **Payment Fee Tax**: Separate calculation for payment method fees if taxable
- **EU VAT**: Special rules for EU cross-border transactions
- **Digital Products**: Different tax rules may apply

**Location**: `Nop.Services.Tax.TaxService.GetProductPrice()`

---

## Access Control Decision Logic

**Question**: Does this user have access to this resource?

```
START: Authorization Check
│
├─> INPUT:
│   ├─> Customer
│   ├─> RequestedPermission (e.g., "ManageProducts")
│   └─> OptionalEntity (for ACL checks)
│
├─> STEP 1: System Admin Check
│   │
│   ├─> Is customer in "Administrators" role?
│   │   └─> YES → GRANT ACCESS (admins have all permissions)
│   │   └─> NO → Continue
│   │
│   └─> Continue
│
├─> STEP 2: Permission Check
│   │
│   ├─> Get all customer's roles:
│   │   └─> CustomerRoles = Customer.CustomerRoles (active only)
│   │
│   ├─> Get permission record:
│   │   └─> Permission = PermissionRecord.GetBySystemName(RequestedPermission)
│   │
│   ├─> Is Permission null?
│   │   └─> YES → DENY ACCESS (permission doesn't exist)
│   │   └─> NO → Continue
│   │
│   ├─> Check if any customer role has this permission:
│   │   │
│   │   ├─> For each CustomerRole in CustomerRoles:
│   │   │   └─> Is Permission in Role.PermissionRecords?
│   │   │       └─> YES → Permission Granted
│   │   │
│   │   ├─> Any role grants permission?
│   │   │   └─> YES → Continue to STEP 3
│   │   │   └─> NO → DENY ACCESS (no role has permission)
│   │
│   └─> Continue
│
├─> STEP 3: Entity-Level ACL Check (if entity provided)
│   │
│   ├─> Does entity implement IAclSupported?
│   │   └─> NO → GRANT ACCESS (no ACL restrictions)
│   │   └─> YES → Continue
│   │
│   ├─> Is entity.SubjectToAcl = false?
│   │   └─> YES → GRANT ACCESS (ACL not enabled for this entity)
│   │   └─> NO → Continue
│   │
│   ├─> Get entity's AclRecords:
│   │   └─> AclRecords = AclRecord.GetByEntity(entity)
│   │
│   ├─> Are there AclRecords?
│   │   └─> NO → DENY ACCESS (ACL enabled but no records = deny all)
│   │   └─> YES → Continue
│   │
│   ├─> Check if customer's role matches any AclRecord:
│   │   │
│   │   ├─> For each AclRecord:
│   │   │   └─> Is AclRecord.CustomerRoleId in customer's roles?
│   │   │       └─> YES → ACL Passed
│   │   │
│   │   ├─> Any ACL record matches?
│   │   │   └─> YES → Continue to STEP 4
│   │   │   └─> NO → DENY ACCESS (role not in ACL)
│   │
│   └─> Continue
│
├─> STEP 4: Store Mapping Check (if entity provided)
│   │
│   ├─> Does entity implement IStoreMappingSupported?
│   │   └─> NO → GRANT ACCESS (no store restrictions)
│   │   └─> YES → Continue
│   │
│   ├─> Is entity.LimitedToStores = false?
│   │   └─> YES → GRANT ACCESS (available in all stores)
│   │   └─> NO → Continue
│   │
│   ├─> Get current store:
│   │   └─> CurrentStore = IStoreContext.CurrentStore
│   │
│   ├─> Get entity's StoreMappings:
│   │   └─> StoreMappings = StoreMapping.GetByEntity(entity)
│   │
│   ├─> Is CurrentStore.Id in StoreMappings?
│   │   └─> YES → GRANT ACCESS
│   │   └─> NO → DENY ACCESS (not available in this store)
│   │
│   └─> Continue
│
├─> STEP 5: Additional Custom Checks (if applicable)
│   │
│   ├─> Vendor-specific checks:
│   │   └─> If customer is vendor, can only access own products
│   │
│   ├─> Multi-store checks:
│   │   └─> Store managers limited to assigned stores
│   │
│   └─> Continue
│
└─> RESULT: GRANT ACCESS or DENY ACCESS (with specific reason)
```

**Implementation**:
- `Nop.Services.Security.PermissionService.Authorize()`
- `Nop.Services.Security.AclService.Authorize()`
- `Nop.Services.Stores.StoreMappingService.Authorize()`

---

## Summary

This decision logic documentation demonstrates:

1. **Complex Conditional Logic**: Multi-level decision trees with numerous evaluation points
2. **Business Rule Enforcement**: Decisions based on configurable business rules
3. **Extensibility**: Plugin system allows custom decision logic
4. **Performance Considerations**: Early exits for common cases
5. **Clear Failure Reasons**: Specific feedback on why decisions fail
6. **Hierarchical Evaluation**: Checks progress from general to specific
7. **State-Based Logic**: Decisions depend on current system state
8. **Role-Based Access**: Fine-grained authorization controls

**Related Documentation**:
- [Business Logic](business-logic.md) - Detailed business rules
- [Workflows](workflows.md) - Process flows incorporating these decisions
- [Error Handling](error-handling.md) - Error scenarios from decision failures

---

**Document Version**: 1.0  
**Analysis Method**: Static code analysis of service layer decision points
