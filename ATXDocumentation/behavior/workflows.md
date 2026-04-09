# nopCommerce Workflows Documentation

## Overview

This document describes the key business process workflows in nopCommerce, detailing the sequence of operations, participants, and decision points for major e-commerce scenarios.

## Table of Contents
- [Customer Registration Workflow](#customer-registration-workflow)
- [Product Browsing and Search Workflow](#product-browsing-and-search-workflow)
- [Shopping Cart Workflow](#shopping-cart-workflow)
- [Checkout Workflow](#checkout-workflow)
- [Order Fulfillment Workflow](#order-fulfillment-workflow)
- [Payment Processing Workflow](#payment-processing-workflow)
- [Return and Refund Workflow](#return-and-refund-workflow)
- [Product Review Workflow](#product-review-workflow)
- [Admin Product Management Workflow](#admin-product-management-workflow)

---

## Customer Registration Workflow

### Process: New Customer Registration

**Participants**: Anonymous Visitor, Customer Service, Authentication Service, Notification Service

**Pre-conditions**: 
- Customer not already logged in
- Registration enabled in settings

**Workflow Steps**:

```
1. START: Customer clicks "Register" link

2. Display Registration Form
   └─> Collect: Email, Password, First Name, Last Name
   └─> Optional: Username (if enabled)
   └─> Optional: Custom attributes (Gender, Date of Birth, Company, etc.)
   
3. Customer Submits Form

4. Validate Input
   ├─> Email format valid? → No → Return error, show form
   ├─> Email unique? → No → Return "Email already registered" error
   ├─> Username unique? (if enabled) → No → Return error
   ├─> Password meets requirements? → No → Return error with requirements
   └─> All valid → Continue

5. Create Customer Account
   ├─> Generate customer GUID
   ├─> Hash password with salt
   ├─> Assign "Registered" customer role
   ├─> Set Active = true
   ├─> Store registration date
   └─> Save to database

6. Migrate Guest Data (if applicable)
   ├─> Transfer shopping cart items
   ├─> Transfer wishlist items
   ├─> Transfer checkout attributes
   └─> Delete guest customer record

7. Authentication
   └─> Create authentication cookie
   └─> Set customer as authenticated

8. Send Welcome Email
   └─> Call WorkflowMessageService.SendCustomerWelcomeMessage()
   └─> Include account details and store information

9. Redirect to Home or Return URL
   
10. END: Customer registered and logged in
```

**Post-conditions**:
- Customer record exists in database
- Customer authenticated with session cookie
- Welcome email sent
- Customer can proceed with shopping

**Implementation**: 
- `Nop.Services.Customers.CustomerRegistrationService.RegisterCustomer()`
- `Nop.Web.Controllers.CustomerController.Register()`

---

## Product Browsing and Search Workflow

### Process: Customer Searches and Filters Products

**Participants**: Customer, Catalog Service, Cache Manager

**Workflow Steps**:

```
1. START: Customer navigates to catalog

2. Customer Action (one of):
   ├─> Browse category
   ├─> Browse manufacturer
   ├─> Search with keywords
   └─> View homepage featured products

3. Apply Initial Filters
   ├─> Store filter (current store only)
   ├─> Published = true
   ├─> Deleted = false
   ├─> Date availability (within AvailableStartDate/EndDate)
   └─> ACL check (customer's roles)

4. Apply Customer-Selected Filters
   ├─> Price range (min/max)
   ├─> Specification attributes (Size, Color, etc.)
   ├─> Product tags
   ├─> Manufacturers
   ├─> Rating
   └─> Availability (In stock only)

5. Check Cache
   ├─> Generate cache key from filter parameters
   ├─> Cache hit? → Yes → Return cached results
   └─> Cache miss? → Continue to database query

6. Execute Database Query
   ├─> IQueryable<Product> with filters
   ├─> Include navigation properties (Pictures, ProductCategories)
   ├─> Apply sorting (Position, Price, Name, Created Date)
   └─> Apply paging (page size, page index)

7. Load Additional Data per Product
   ├─> Load product pictures
   ├─> Calculate final price (with discounts)
   ├─> Check inventory status
   ├─> Load review summary (average rating, count)
   └─> Apply localization (name, description)

8. Cache Results
   └─> Store in cache with expiration time

9. Return Product List
   └─> Include paging info (total count, page number)

10. Render Product Grid/List
    ├─> Display product images
    ├─> Display names and prices
    ├─> Display "Add to Cart" buttons
    ├─> Display ratings and reviews
    └─> Display stock availability

11. END: Customer sees filtered product list
```

**Implementation**:
- `Nop.Services.Catalog.ProductService.SearchProducts()`
- `Nop.Web.Controllers.CatalogController`

---

## Shopping Cart Workflow

### Process: Add Product to Shopping Cart

**Participants**: Customer, Product Service, Shopping Cart Service, Price Calculation Service

**Workflow Steps**:

```
1. START: Customer clicks "Add to Cart" on product

2. Product Type Check
   ├─> Simple Product → Continue
   ├─> Grouped Product → Show associated products, customer selects
   └─> Product with Attributes → Show attribute selection form

3. Customer Selects Options (if applicable)
   ├─> Required attributes (Size, Color, etc.)
   ├─> Quantity
   ├─> Rental dates (if rental product)
   └─> Custom entered price (if allowed)

4. Validate Selection
   ├─> All required attributes selected? → No → Show error
   ├─> Quantity > 0? → No → Show error
   ├─> Attribute combination exists? → No → Show "Not available" error
   └─> All valid → Continue

5. Check Inventory
   ├─> Stock management enabled?
   │   ├─> Yes → Check available quantity
   │   │   ├─> Sufficient stock? → Yes → Continue
   │   │   └─> Insufficient stock? → No → Show error, offer backorder
   │   └─> No → Continue (unlimited stock)

6. Check Cart Constraints
   ├─> Maximum cart quantity exceeded? → Yes → Show error
   ├─> Minimum order quantity met? → No → Show error
   └─> All checks pass → Continue

7. Check for Existing Cart Item
   ├─> Same product with same attributes in cart?
   │   ├─> Yes → Update quantity (add to existing)
   │   └─> No → Create new cart item

8. Calculate Prices
   ├─> Get unit price (with attributes, discounts, tier pricing)
   ├─> Calculate subtotal (unit price × quantity)
   └─> Store prices in cart item

9. Save Shopping Cart Item
   ├─> Set CustomerID
   ├─> Set ProductID
   ├─> Set AttributesXML
   ├─> Set Quantity
   ├─> Set ShoppingCartType (ShoppingCart or Wishlist)
   ├─> Set StoreID
   └─> Insert/Update in database

10. Publish Event
    └─> ShoppingCartItemAddedEvent

11. Update Cart Display
    ├─> Refresh cart icon (item count)
    ├─> Update cart totals
    └─> Show success message

12. Optional: Show Mini Cart Popup
    ├─> Display added item
    ├─> Show cart summary
    └─> Provide "Continue Shopping" or "Checkout" options

13. END: Product added to cart
```

**Implementation**:
- `Nop.Services.Orders.ShoppingCartService.AddToCart()`
- `Nop.Web.Controllers.ShoppingCartController.AddProductToCart_Details()`

---

## Checkout Workflow

### Process: Complete Order Checkout

**Participants**: Customer, Order Processing Service, Payment Service, Shipping Service, Tax Service

**Workflow Steps**:

```
1. START: Customer clicks "Checkout" from cart

2. Authentication Check
   ├─> Customer logged in? → No → Redirect to login/registration
   └─> Yes → Continue

3. Shopping Cart Validation
   ├─> Cart has items? → No → Redirect to cart page
   ├─> All items available? → No → Show errors, update cart
   ├─> Sufficient inventory? → No → Show errors
   └─> All valid → Continue

4. STEP 1: Billing Address
   ├─> Load existing addresses (if any)
   ├─> Customer selects existing OR enters new address
   ├─> Validate address fields (required fields present)
   ├─> Save new address to customer account
   └─> Store billing address ID in checkout state

5. STEP 2: Shipping Address
   ├─> Use billing address for shipping? → Yes → Skip to step 3
   ├─> No → Load existing addresses
   ├─> Customer selects or enters shipping address
   ├─> Validate address fields
   ├─> Save address
   └─> Store shipping address ID

6. STEP 3: Shipping Method
   ├─> Get available shipping options
   │   ├─> Call shipping rate computation methods
   │   ├─> Filter by country restrictions
   │   ├─> Filter by weight/dimension limits
   │   └─> Return list of options with rates
   ├─> Customer selects shipping method
   ├─> Calculate shipping cost
   └─> Store selected method

7. STEP 4: Payment Method
   ├─> Get available payment methods
   │   ├─> Filter by country
   │   ├─> Filter by customer role
   │   ├─> Filter by cart compatibility
   │   └─> Return list of payment options
   ├─> Customer selects payment method
   ├─> Display payment information form (if needed)
   │   ├─> Credit card form
   │   └─> OR redirect to external gateway
   ├─> Customer enters payment details
   ├─> Validate payment information
   └─> Store payment method and details

8. STEP 5: Checkout Attributes (if configured)
   ├─> Display custom checkout attributes (gift wrapping, special instructions)
   ├─> Customer selects/enters values
   └─> Store attribute selections

9. STEP 6: Order Review
   ├─> Display complete order summary
   │   ├─> Products and quantities
   │   ├─> Billing/shipping addresses
   │   ├─> Shipping method and cost
   │   ├─> Payment method
   │   ├─> Applied discounts/coupons
   │   ├─> Tax amounts
   │   └─> Grand total
   ├─> Customer reviews and accepts terms of service
   └─> Customer clicks "Confirm Order"

10. Final Validation
    ├─> Re-validate cart (items still available, prices unchanged)
    ├─> Re-validate addresses
    ├─> Re-validate shipping method
    ├─> Re-validate payment method
    └─> All valid → Continue (else show errors and return to review)

11. Process Payment
    ├─> Create ProcessPaymentRequest with order details
    ├─> Call PaymentService.ProcessPayment()
    │   ├─> Payment plugin processes transaction
    │   ├─> Return status: Authorized, Paid, Pending, or Failed
    └─> Handle payment result
        ├─> Failed → Show error, allow retry
        └─> Success → Continue

12. Create Order
    ├─> Generate order GUID
    ├─> Generate order number
    ├─> Create Order entity
    │   ├─> Copy customer information (snapshot)
    │   ├─> Copy addresses (snapshot)
    │   ├─> Set totals (subtotal, shipping, tax, total)
    │   ├─> Set payment information
    │   ├─> Set shipping information
    │   └─> Set order status (Pending or Processing)
    ├─> Create OrderItem entities
    │   ├─> Snapshot product prices
    │   ├─> Store attributes
    │   └─> Set quantities
    ├─> Apply and record discount usage
    ├─> Generate gift cards (if applicable)
    └─> Save order to database

13. Reduce Inventory
    └─> Decrease StockQuantity for each product

14. Clear Shopping Cart
    └─> Delete cart items for customer

15. Post-Payment Processing
    ├─> Payment method requires redirect? (PayPal, external gateway)
    │   ├─> Yes → Redirect to payment gateway
    │   └─> No → Continue
    └─> Store payment transaction ID

16. Send Notifications
    ├─> Send order confirmation email to customer
    ├─> Send order notification to store owner
    └─> Send order notification to vendors (if multi-vendor)

17. Post-Order Tasks
    ├─> Record affiliate commission (if affiliate link used)
    ├─> Award reward points (if enabled)
    ├─> Create recurring payment schedule (if subscription)
    └─> Publish OrderPlacedEvent

18. Display Order Confirmation Page
    ├─> Show order number
    ├─> Show order details
    └─> Provide link to order tracking

19. END: Order placed successfully
```

**Alternate Flows**:
- **Payment Failed**: Return to payment method step, allow retry or different method
- **Inventory Depleted Mid-Checkout**: Show error, update cart, restart checkout
- **External Payment Gateway**: Redirect to gateway → Customer completes payment → Return to site → Process callback → Create order

**Implementation**:
- `Nop.Web.Controllers.CheckoutController`
- `Nop.Services.Orders.OrderProcessingService.PlaceOrder()`

---

## Order Fulfillment Workflow

### Process: Admin Processes and Ships Order

**Participants**: Admin, Order Service, Shipment Service, Notification Service

**Workflow Steps**:

```
1. START: Admin views order list

2. Filter/Search Orders
   ├─> By status (Pending, Processing)
   ├─> By date range
   ├─> By customer
   └─> By payment/shipping status

3. Select Order to Fulfill
   └─> Admin clicks order to view details

4. Order Detail Review
   ├─> View customer information
   ├─> View ordered products
   ├─> View payment status
   ├─> View shipping address
   └─> View order notes

5. Payment Verification
   ├─> Payment status = Authorized?
   │   ├─> Yes → Admin captures payment
   │   │   ├─> Call payment gateway to capture
   │   │   └─> Update order payment status to Paid
   │   └─> Already Paid → Continue
   └─> Payment failed → Handle separately (cancel or retry)

6. Mark Order as Processing
   └─> Update OrderStatus = Processing

7. Prepare Shipment
   ├─> Admin clicks "Ship" button
   ├─> Select products/quantities to ship
   │   └─> For partial shipments, select subset of items
   ├─> Enter tracking number (optional)
   ├─> Select warehouse (if multi-warehouse)
   └─> Confirm shipment creation

8. Create Shipment Record
   ├─> Generate shipment entity
   ├─> Link shipment to order
   ├─> Record shipped items and quantities
   ├─> Store tracking number
   ├─> Set ShippedDateUtc
   ├─> Set shipping method from order
   └─> Save to database

9. Reduce Inventory (if not done at order time)
   └─> Decrease warehouse stock for shipped items

10. Update Order Status
    ├─> All items shipped? 
    │   ├─> Yes → Set ShippingStatus = Shipped
    │   └─> No → Set ShippingStatus = PartiallyShipped
    └─> Save order

11. Send Shipment Notification
    ├─> Call WorkflowMessageService.SendShipmentSentCustomerNotification()
    ├─> Email includes:
    │   ├─> Tracking number
    │   ├─> Shipped items
    │   └─> Carrier information
    └─> Send email to customer

12. Publish Event
    └─> ShipmentSentEvent

13. Track Shipment (ongoing)
    ├─> Admin or customer can view tracking
    ├─> Link to carrier tracking page
    └─> Monitor delivery status

14. Mark as Delivered (when arrived)
    ├─> Admin clicks "Mark as Delivered"
    ├─> Set DeliveryDateUtc
    ├─> Update ShippingStatus = Delivered
    └─> Send delivery notification email

15. Complete Order
    ├─> All items delivered?
    ├─> Payment complete?
    └─> Mark OrderStatus = Complete

16. END: Order fulfilled and delivered
```

**Implementation**:
- `Nop.Admin.Controllers.OrderController`
- `Nop.Services.Shipping.ShipmentService`
- `Nop.Services.Orders.OrderProcessingService`

---

## Payment Processing Workflow

### Process: Authorize and Capture Payment

**Participants**: Customer, Payment Service, Payment Gateway Plugin, Order Processing Service

**Workflow Steps**:

```
1. START: Payment processing initiated at checkout

2. Prepare Payment Request
   ├─> Create ProcessPaymentRequest object
   ├─> Set order total
   ├─> Set customer information
   ├─> Set payment method system name
   ├─> Set credit card details (if applicable)
   └─> Set billing address

3. Select Payment Plugin
   └─> Load IPaymentMethod by system name

4. Payment Type Determination
   ├─> PaymentMethodType = Standard → Authorize and/or capture
   ├─> PaymentMethodType = Redirection → External gateway redirect
   └─> PaymentMethodType = Button → JavaScript-based (PayPal button)

5. AUTHORIZE ONLY Path:
   ├─> Call paymentMethod.ProcessPayment(request)
   ├─> Payment gateway authorizes transaction
   │   ├─> Verify card/account details
   │   ├─> Check available funds
   │   └─> Place hold on funds
   ├─> Gateway returns authorization code
   ├─> Store authorization transaction ID
   ├─> Set PaymentStatus = Authorized
   └─> Order created in Authorized state
   
   └─> Later: Admin Captures Payment
       ├─> Admin clicks "Capture" in order details
       ├─> Create CapturePaymentRequest
       ├─> Call paymentMethod.Capture(request)
       ├─> Gateway transfers funds to merchant
       ├─> Update PaymentStatus = Paid
       └─> Complete order

6. AUTHORIZE AND CAPTURE Path:
   ├─> Call paymentMethod.ProcessPayment(request)
   ├─> Payment gateway authorizes and charges
   │   ├─> Verify card/account details
   │   └─> Transfer funds immediately
   ├─> Gateway returns transaction ID
   ├─> Store capture transaction ID
   ├─> Set PaymentStatus = Paid
   └─> Order created in Paid state

7. REDIRECTION Path:
   ├─> Store temporary payment data in session
   ├─> Redirect customer to payment gateway
   ├─> Customer completes payment on external site
   ├─> Gateway redirects back with result
   ├─> Process return/callback URL
   │   ├─> Validate response signature
   │   ├─> Retrieve payment status
   │   └─> Update order accordingly
   └─> Display result to customer

8. Handle Payment Result
   ├─> Success (Authorized or Paid)
   │   ├─> Store transaction ID
   │   ├─> Store authorization code
   │   ├─> Set payment status
   │   └─> Continue order creation
   │
   ├─> Pending (async payment)
   │   ├─> Create order in Pending status
   │   ├─> Wait for gateway webhook/callback
   │   └─> Update order when status changes
   │
   └─> Failed
       ├─> Log error message
       ├─> Display error to customer
       ├─> Do NOT create order
       ├─> Restore inventory (if reserved)
       └─> Allow customer to retry

9. Post-Process Payment (if needed)
   ├─> Call paymentMethod.PostProcessPayment(request)
   ├─> Used for additional steps (redirect, logging)
   └─> Payment method specific handling

10. Record Payment History
    ├─> Store transaction details
    ├─> Store gateway response
    ├─> Link to order
    └─> Audit trail for compliance

11. END: Payment processed
```

**Refund Scenario**:
```
1. Admin initiates refund from order details
2. Create RefundPaymentRequest
   ├─> Set order reference
   ├─> Set refund amount (full or partial)
   └─> Set reason (optional)
3. Call paymentMethod.Refund(request)
4. Gateway processes refund
   ├─> Returns funds to customer
   └─> Returns success/failure
5. Update order
   ├─> Set PaymentStatus = Refunded (or PartiallyRefunded)
   ├─> Record refund amount and date
   └─> Add order note with refund details
6. Send refund notification to customer
7. Optionally restore inventory
```

**Implementation**:
- `Nop.Services.Payments.PaymentService`
- Payment plugins in `Nop.Plugin.Payments.*`

---

## Return and Refund Workflow

### Process: Customer Returns Product

**Participants**: Customer, Return Request Service, Order Service, Admin

**Workflow Steps**:

```
1. START: Customer wants to return product

2. Customer Initiates Return
   ├─> Login to account
   ├─> Navigate to "My Orders"
   ├─> Click order to view details
   └─> Click "Return Items" button

3. Check Return Eligibility
   ├─> Order status = Complete? → No → Cannot return yet
   ├─> Within return window (X days after delivery)? → No → Show "return period expired"
   ├─> Product marked as non-returnable? → Yes → Cannot return
   └─> All checks pass → Continue

4. Customer Completes Return Request Form
   ├─> Select products to return (checkboxes)
   ├─> Select quantity per product
   ├─> Select return reason (dropdown)
   │   └─> Options: Wrong product, Defective, Not as described, etc.
   ├─> Select return action (dropdown)
   │   └─> Options: Refund, Store credit, Exchange
   ├─> Enter comments/details (textarea)
   └─> Submit form

5. Validate Return Request
   ├─> At least one product selected? → No → Show error
   ├─> Quantities valid? → No → Show error
   ├─> Reason selected? → No → Show error
   └─> All valid → Continue

6. Create Return Request Record
   ├─> Generate return request ID
   ├─> Link to order and customer
   ├─> Store selected products and quantities
   ├─> Store return reason and action
   ├─> Store comments
   ├─> Set Status = Pending
   ├─> Set RequestDateUtc
   └─> Save to database

7. Send Notifications
   ├─> Email customer: Return request received, includes return ID
   └─> Email store owner: New return request notification

8. Display Confirmation to Customer
   ├─> Show return request number
   ├─> Provide return instructions
   ├─> Show return shipping address (if applicable)
   └─> Provide tracking information form

9. Admin Reviews Return Request
   ├─> Navigate to Returns management
   ├─> View pending requests
   ├─> Select request to review
   ├─> View order details, products, customer history
   └─> Make decision

10. Admin Approves or Rejects
    ├─> APPROVED:
    │   ├─> Update Status = Approved
    │   ├─> Add admin comments
    │   ├─> Send approval email to customer
    │   └─> Provide return authorization number
    │
    └─> REJECTED:
        ├─> Update Status = Rejected
        ├─> Add rejection reason
        ├─> Send rejection email to customer
        └─> END (no further processing)

11. Customer Ships Product Back
    ├─> Package items
    ├─> Include return authorization number
    ├─> Ship to return address
    └─> Optionally update return request with tracking number

12. Admin Receives Returned Product
    ├─> Inspect product condition
    ├─> Verify against return request
    └─> Update Status = ItemReceived

13. Process Return
    ├─> Requested Action = Refund:
    │   ├─> Initiate refund through payment gateway
    │   ├─> Process refund (see Payment Processing Workflow)
    │   ├─> Update order PaymentStatus
    │   └─> Send refund confirmation email
    │
    ├─> Requested Action = Store Credit:
    │   ├─> Generate store credit (gift card)
    │   ├─> Send gift card code to customer
    │   └─> Update return request
    │
    └─> Requested Action = Exchange:
        ├─> Create replacement order
        ├─> Ship replacement product
        └─> Link to original return request

14. Restore Inventory
    └─> Increase StockQuantity for returned product (if product resellable)

15. Close Return Request
    ├─> Update Status = Complete
    ├─> Set CompletedDateUtc
    ├─> Add final admin notes
    └─> Archive request

16. END: Return processed
```

**Implementation**:
- `Nop.Services.Orders.ReturnRequestService`
- `Nop.Web.Controllers.ReturnRequestController`
- `Nop.Admin.Controllers.ReturnRequestController`

---

## Product Review Workflow

### Process: Customer Submits Product Review

**Participants**: Customer, Product Service, Notification Service, Admin (for moderation)

**Workflow Steps**:

```
1. START: Customer views product details

2. Check Review Eligibility
   ├─> Customer logged in? → No → Show "login to review" message
   ├─> Product allows reviews? → No → Don't show review form
   ├─> Customer already reviewed this product? → Yes → Show existing review, option to edit
   └─> All checks pass → Show review form

3. Customer Completes Review Form
   ├─> Enter review title (required)
   ├─> Enter review text (required)
   ├─> Select rating (1-5 stars, required)
   └─> Submit review

4. Validate Review
   ├─> Title not empty? → No → Show error
   ├─> Review text not empty? → No → Show error
   ├─> Rating selected? → No → Show error
   ├─> Text meets minimum length? → No → Show error
   └─> All valid → Continue

5. Check Moderation Settings
   ├─> Reviews require approval?
   │   ├─> Yes → Set IsApproved = false, show "pending" message
   │   └─> No → Set IsApproved = true, publish immediately
   
6. Create Product Review Record
   ├─> Set ProductId
   ├─> Set CustomerId
   ├─> Set Rating (1-5)
   ├─> Set Title
   ├─> Set ReviewText
   ├─> Set IsApproved (based on settings)
   ├─> Set CreatedOnUtc
   ├─> Set HelpfulYesTotal = 0
   ├─> Set HelpfulNoTotal = 0
   └─> Save to database

7. Send Notification
   └─> Email admin: New product review submitted

8. Display Message to Customer
   ├─> If approved: "Thank you for your review"
   └─> If pending: "Your review will be published after moderation"

9. Admin Moderation (if required)
   ├─> Admin navigates to Product Reviews
   ├─> Filter by "Not Approved"
   ├─> Review submitted content
   ├─> Check for spam, inappropriate content
   └─> Decision:
       ├─> APPROVE:
       │   ├─> Set IsApproved = true
       │   ├─> Update product review totals
       │   └─> Publish review on product page
       │
       └─> REJECT/DELETE:
           ├─> Delete review record
           └─> Optionally notify customer

10. Update Product Review Summary
    ├─> Recalculate average rating
    ├─> Update total approved review count
    ├─> Update Product.ApprovedRatingSum
    ├─> Update Product.ApprovedTotalReviews
    └─> Cache new values

11. Display Review on Product Page
    ├─> Show on reviews tab
    ├─> Include customer name (or anonymous)
    ├─> Include rating stars
    ├─> Include date submitted
    ├─> Show "Was this helpful?" voting buttons
    └─> Update product rating summary at top

12. Other Customers Vote on Helpfulness
    ├─> Click "Yes, this was helpful"
    │   └─> Increment HelpfulYesTotal
    └─> Click "No, this was not helpful"
        └─> Increment HelpfulNoTotal

13. END: Review published and visible
```

**Implementation**:
- `Nop.Services.Catalog.ProductService` (review methods)
- `Nop.Web.Controllers.ProductController.ProductReviewsAdd()`
- `Nop.Admin.Controllers.ProductReviewController`

---

## Admin Product Management Workflow

### Process: Admin Creates New Product

**Participants**: Admin, Product Service, Picture Service

**Workflow Steps**:

```
1. START: Admin navigates to Products page

2. Click "Add New Product" Button

3. Complete Product Information Form
   └─> TAB 1: Product Info
       ├─> Product name (required)
       ├─> Short description
       ├─> Full description (rich text)
       ├─> SKU (stock keeping unit)
       ├─> Product type (Simple, Grouped, etc.)
       ├─> Published (checkbox)
       ├─> Product template
       ├─> Vendor (if multi-vendor)
       └─> Show on homepage (checkbox)
   
   └─> TAB 2: Pricing
       ├─> Price (required)
       ├─> Old price (for showing discount)
       ├─> Product cost
       ├─> Disable buy button
       ├─> Disable wishlist button
       ├─> Available for pre-order
       ├─> Call for price (hide price, show contact form)
       ├─> Customer enters price (name-your-price)
       └─> PAngularJS configuration

   └─> TAB 3: Categories
       ├─> Select categories (multi-select)
       ├─> Set display order per category
       └─> Mark as featured product per category
   
   └─> TAB 4: Manufacturers
       ├─> Select manufacturers
       └─> Set display order
   
   └─> TAB 5: Pictures
       ├─> Upload product images
       ├─> Set main picture
       ├─> Set display order for gallery
       └─> Add alt text for SEO
   
   └─> TAB 6: Inventory
       ├─> Manage inventory method (dropdown)
       ├─> Stock quantity
       ├─> Warehouse (if multi-warehouse)
       ├─> Display stock availability
       ├─> Min/max order quantities
       ├─> Allowed quantities (dropdown)
       ├─> Not returnable (checkbox)
       └─> Low stock activity
   
   └─> TAB 7: Shipping
       ├─> Requires shipping (checkbox)
       ├─> Free shipping (checkbox)
       ├─> Ship separately (checkbox)
       ├─> Additional shipping charge
       ├─> Weight
       ├─> Dimensions (L × W × H)
       └─> Delivery date options
   
   └─> TAB 8: Attributes
       ├─> Add product attributes
       ├─> Configure attribute options
       ├─> Set price adjustments
       ├─> Required vs optional
       └─> Define attribute combinations
   
   └─> TAB 9: Specification Attributes
       ├─> Add specs (Color, Size, Material, etc.)
       ├─> Set values
       └─> Show on product page
   
   └─> TAB 10: SEO
       ├─> Meta title
       ├─> Meta description
       ├─> Meta keywords
       └─> SEO-friendly URL slug

4. Validate Product Data
   ├─> Name not empty? → No → Show error
   ├─> Price valid? → No → Show error
   ├─> SKU unique (if required)? → No → Show error
   └─> All required fields complete → Continue

5. Save Product
   ├─> Create Product entity
   ├─> Generate product GUID
   ├─> Set all property values
   ├─> Set CreatedOnUtc
   ├─> Set UpdatedOnUtc
   └─> Insert into database

6. Save Related Data
   ├─> Save ProductCategory mappings
   ├─> Save ProductManufacturer mappings
   ├─> Save ProductPicture mappings
   ├─> Save ProductAttribute mappings
   ├─> Save SpecificationAttribute mappings
   └─> Save UrlRecord for SEO slug

7. Upload and Process Pictures
   ├─> Resize images to configured sizes
   ├─> Generate thumbnails
   ├─> Store in /content/images/thumbs/
   ├─> Create Picture records
   └─> Link to product

8. Configure Attribute Combinations (if applicable)
   ├─> Admin navigates to Attribute Combinations tab
   ├─> Generate all combinations (auto) or add manually
   ├─> For each combination:
   │   ├─> Set unique SKU (optional)
   │   ├─> Set price adjustment
   │   ├─> Set stock quantity
   │   ├─> Set picture
   │   └─> Mark as available/unavailable
   └─> Save combinations

9. Configure Tier Prices (if applicable)
   ├─> Add tier pricing rules
   ├─> Set quantity thresholds
   ├─> Set discounted prices
   ├─> Optionally limit to customer roles
   └─> Save tier prices

10. Publish Event
    └─> ProductInsertedEvent

11. Clear Caches
    ├─> Clear product cache
    ├─> Clear category cache
    └─> Clear pricing cache

12. Display Success Message
    └─> "Product created successfully"

13. Admin Options
    ├─> Continue editing
    ├─> Save and add another product
    └─> Return to product list

14. END: Product created and available (if published)
```

**Update Product Workflow**:
- Similar to create, but loads existing product
- Tracks modification history
- Publishes `ProductUpdatedEvent`
- Maintains audit trail

**Delete Product Workflow**:
- Soft delete (set `Deleted = true`) rather than hard delete
- Preserves order history
- Removes from storefront
- Can be restored by admin

**Implementation**:
- `Nop.Admin.Controllers.ProductController`
- `Nop.Services.Catalog.ProductService`
- `Nop.Services.Seo.UrlRecordService`

---

## Summary

These workflows demonstrate:

1. **Multi-Step Processes**: Complex operations broken into clear stages
2. **Validation at Every Step**: Input validation, business rule checks, constraint verification
3. **State Management**: Proper state transitions with validation
4. **Notification Integration**: Emails sent at appropriate workflow points
5. **Error Handling**: Graceful failure with user-friendly messages
6. **Audit Trail**: Events and logging throughout processes
7. **Flexibility**: Multiple paths and options within workflows
8. **User Experience**: Clear feedback and guidance at each step

**Related Documentation**:
- [Business Logic](business-logic.md) - Detailed business rules
- [Decision Logic](decision-logic.md) - Conditional logic and decision trees
- [Error Handling](error-handling.md) - Exception and validation patterns

---

**Document Version**: 1.0  
**Analysis Method**: Static code analysis of controllers and services
