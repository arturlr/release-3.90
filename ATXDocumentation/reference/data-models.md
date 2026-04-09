# nopCommerce Data Models Reference

## Table of Contents
- [Overview](#overview)
- [Core Domain Model](#core-domain-model)
- [Catalog Domain](#catalog-domain)
- [Customer Domain](#customer-domain)
- [Order Domain](#order-domain)
- [Content Domain](#content-domain)
- [Configuration and Settings](#configuration-and-settings)
- [Entity Relationships](#entity-relationships)

## Overview

nopCommerce implements a rich domain model with over 100 entity types covering all aspects of e-commerce operations. All entities inherit from `BaseEntity` which provides a common integer `Id` property and proper equality semantics.

**Location**: `Nop.Core.Domain.*`

**Base Entity**: All domain entities derive from `Nop.Core.BaseEntity`

```csharp
public abstract partial class BaseEntity
{
    public int Id { get; set; }
    // Equality implementation based on Id
}
```

## Core Domain Model

### BaseEntity Characteristics
- Integer primary key (`Id`)
- Proper equality comparison (by ID for persisted entities, by reference for transient)
- Support for Entity Framework proxy types
- Operators overloaded for equality comparison

### Common Entity Patterns

#### Auditable Entities
Many entities include audit fields:
- `CreatedOnUtc` - Creation timestamp (UTC)
- `UpdatedOnUtc` - Last modification timestamp (UTC)

#### Soft Delete Pattern
Many entities use soft delete:
- `Deleted` - Boolean flag instead of physical deletion

#### Publishing Pattern
Content entities often include:
- `Published` - Visibility flag
- `AvailableStartDateTimeUtc` - Publication start date
- `AvailableEndDateTimeUtc` - Publication end date

## Catalog Domain

**Location**: `Nop.Core.Domain.Catalog`

### Product
The central entity for e-commerce operations with 100+ properties.

**Key Property Groups**:

**Basic Information**:
- `Name` - Product name (required, max 400 chars)
- `ShortDescription` - Brief description
- `FullDescription` - Detailed HTML description
- `Sku` - Stock keeping unit (max 400 chars)
- `ManufacturerPartNumber` - MPN
- `Gtin` - Global Trade Item Number (UPC/EAN/JAN/ISBN)
- `AdminComment` - Internal notes

**Product Type and Organization**:
- `ProductTypeId` / `ProductType` enum - Simple, Grouped
- `ParentGroupedProductId` - For grouped products
- `VisibleIndividually` - Catalog/search visibility
- `ProductTemplateId` - Display template
- `VendorId` - Vendor/supplier

**Pricing**:
- `Price` - Base price (decimal, 18,4 precision)
- `OldPrice` - Original price for comparison
- `ProductCost` - Cost basis
- `CustomerEntersPrice` - Customer-entered pricing flag
- `MinimumCustomerEnteredPrice` / `MaximumCustomerEnteredPrice`
- `CallForPrice` - Hide price, show "call" message

**Inventory Management**:
- `ManageInventoryMethodId` - Don't manage, by product, by attributes
- `StockQuantity` - Current stock level
- `UseMultipleWarehouses` - Multi-warehouse flag
- `WarehouseId` - Primary warehouse
- `DisplayStockAvailability` / `DisplayStockQuantity`
- `MinStockQuantity` - Low stock threshold
- `LowStockActivityId` - Action when low stock
- `NotifyAdminForQuantityBelow` - Admin notification threshold
- `BackorderModeId` - No backorders, allow, notify customer
- `AllowBackInStockSubscriptions`
- `OrderMinimumQuantity` / `OrderMaximumQuantity`
- `AllowedQuantities` - Comma-separated allowed quantities
- `NotReturnable` - Return policy flag

**Shipping**:
- `IsShipEnabled` - Requires shipping
- `IsFreeShipping` - Free shipping flag
- `ShipSeparately` - Each item ships separately
- `AdditionalShippingCharge` - Extra shipping cost
- `DeliveryDateId` - Delivery timeframe
- `Weight`, `Length`, `Width`, `Height` - Dimensions

**Tax**:
- `IsTaxExempt` - Tax exemption flag
- `TaxCategoryId` - Tax category
- `IsTelecommunicationsOrBroadcastingOrElectronicServices` - EU VAT flag

**SEO**:
- `MetaKeywords` (max 400 chars)
- `MetaDescription`
- `MetaTitle` (max 400 chars)

**Download Products**:
- `IsDownload` - Downloadable product flag
- `DownloadId` - File reference
- `UnlimitedDownloads` / `MaxNumberOfDownloads`
- `DownloadExpirationDays`
- `DownloadActivationTypeId` - When download is available
- `HasSampleDownload` / `SampleDownloadId`
- `HasUserAgreement` / `UserAgreementText`

**Recurring Products**:
- `IsRecurring`
- `RecurringCycleLength` - Period length
- `RecurringCyclePeriodId` - Days/weeks/months/years
- `RecurringTotalCycles` - Number of cycles

**Rental Products**:
- `IsRental`
- `RentalPriceLength` - Rental period length
- `RentalPricePeriodId` - Days/weeks/months/years

**Gift Cards**:
- `IsGiftCard`
- `GiftCardTypeId` - Virtual/Physical
- `OverriddenGiftCardAmount` - Fixed gift amount

**Product Dependencies**:
- `RequireOtherProducts` - Requires other products
- `RequiredProductIds` - Comma-separated IDs
- `AutomaticallyAddRequiredProducts`

**Display Settings**:
- `ShowOnHomePage` - Feature on homepage
- `DisplayOrder` - Sort order
- `Published` - Visibility
- `Deleted` - Soft delete
- `AvailableStartDateTimeUtc` / `AvailableEndDateTimeUtc`

**Pre-order**:
- `AvailableForPreOrder`
- `PreOrderAvailabilityStartDateTimeUtc`

**Pricing Display**:
- `DisableBuyButton` / `DisableWishlistButton`
- `BasepriceEnabled` - German PAngV pricing
- `BasepriceAmount`, `BasepriceUnitId`, `BasepriceBaseAmount`, `BasepriceBaseUnitId`

**New Product Marking**:
- `MarkAsNew`
- `MarkAsNewStartDateTimeUtc` / `MarkAsNewEndDateTimeUtc`

**Performance Optimization Flags**:
- `HasTierPrices` - Avoid loading tier prices if none
- `HasDiscountsApplied` - Avoid loading discounts if none

**Reviews**:
- `AllowCustomerReviews`
- `ApprovedRatingSum` / `NotApprovedRatingSum`
- `ApprovedTotalReviews` / `NotApprovedTotalReviews`

**Multi-Store/ACL**:
- `SubjectToAcl` - Access control
- `LimitedToStores` - Store-specific

**Navigation Properties**:
- `ProductCategories` - Category associations
- `ProductManufacturers` - Manufacturer associations
- `ProductPictures` - Product images (ordered)
- `ProductReviews` - Customer reviews
- `ProductSpecificationAttributes` - Specifications
- `ProductTags` - Tags for grouping/searching
- `ProductAttributeMappings` - Variant attributes
- `ProductAttributeCombinations` - Specific variants
- `TierPrices` - Volume pricing
- `AppliedDiscounts` - Discounts
- `ProductWarehouseInventory` - Multi-warehouse stock

**Interfaces Implemented**:
- `ILocalizedEntity` - Localization support
- `ISlugSupported` - SEO-friendly URLs
- `IAclSupported` - Access control lists
- `IStoreMappingSupported` - Multi-store support

### Category
**Purpose**: Product organization hierarchy

**Key Properties**:
- `Name` (required, max 400 chars)
- `Description`
- `CategoryTemplateId` - Display template
- `MetaKeywords`, `MetaDescription`, `MetaTitle` - SEO
- `ParentCategoryId` - Hierarchical structure
- `PictureId` - Category image
- `PageSize` - Products per page
- `AllowCustomersToSelectPageSize` / `PageSizeOptions`
- `ShowOnHomePage` - Homepage visibility
- `IncludeInTopMenu` - Navigation menu
- `Published`, `Deleted`
- `DisplayOrder`
- `CreatedOnUtc`, `UpdatedOnUtc`
- `SubjectToAcl`, `LimitedToStores`

**Interfaces**: `ILocalizedEntity`, `ISlugSupported`, `IAclSupported`, `IStoreMappingSupported`

### Manufacturer
**Purpose**: Product brands/manufacturers

**Key Properties**:
- `Name` (required, max 400 chars)
- `Description`
- `ManufacturerTemplateId`
- `MetaKeywords`, `MetaDescription`, `MetaTitle`
- `PictureId` - Brand logo
- `PageSize`, `AllowCustomersToSelectPageSize`, `PageSizeOptions`
- `Published`, `Deleted`
- `DisplayOrder`
- `CreatedOnUtc`, `UpdatedOnUtc`
- `SubjectToAcl`, `LimitedToStores`

**Interfaces**: `ILocalizedEntity`, `ISlugSupported`, `IAclSupported`, `IStoreMappingSupported`

### ProductCategory
**Purpose**: Many-to-many relationship between products and categories

**Key Properties**:
- `ProductId`, `CategoryId`
- `IsFeaturedProduct` - Featured in category
- `DisplayOrder` - Sort order within category

### ProductManufacturer
**Purpose**: Many-to-many relationship between products and manufacturers

**Key Properties**:
- `ProductId`, `ManufacturerId`
- `IsFeaturedProduct`
- `DisplayOrder`

### ProductAttribute
**Purpose**: Attribute definitions (Color, Size, etc.)

**Key Properties**:
- `Name` (required)
- `Description`

### ProductAttributeMapping
**Purpose**: Assigns attributes to specific products

**Key Properties**:
- `ProductId`
- `ProductAttributeId`
- `TextPrompt` - Label for customer
- `IsRequired`
- `AttributeControlTypeId` - Dropdown, radio, checkboxes, etc.
- `DisplayOrder`
- `ValidationMinLength`, `ValidationMaxLength`
- `ValidationFileAllowedExtensions`, `ValidationFileMaximumSize`
- `DefaultValue`
- `ConditionAttributeXml` - Conditional display

### ProductAttributeValue
**Purpose**: Specific values for product attributes

**Key Properties**:
- `ProductAttributeMappingId`
- `AttributeValueTypeId` - Simple, associated product
- `AssociatedProductId` - For product bundles
- `Name` (required)
- `ColorSquaresRgb` - Color swatch
- `ImageSquaresPictureId` - Image swatch
- `PriceAdjustment` - Price modification
- `WeightAdjustment`
- `Cost`
- `CustomerEntersQty` - Customer enters quantity
- `Quantity` - Quantity of associated product
- `IsPreSelected`
- `DisplayOrder`
- `PictureId`

### ProductAttributeCombination
**Purpose**: Pre-defined attribute combinations with specific SKU/price/stock

**Key Properties**:
- `ProductId`
- `AttributesXml` - Serialized attribute selection
- `StockQuantity`
- `AllowOutOfStockOrders`
- `Sku`
- `ManufacturerPartNumber`
- `Gtin`
- `OverriddenPrice`
- `NotifyAdminForQuantityBelow`
- `PictureId`

### ProductPicture
**Purpose**: Associate pictures with products

**Key Properties**:
- `ProductId`
- `PictureId`
- `DisplayOrder`

### ProductReview
**Purpose**: Customer product reviews

**Key Properties**:
- `CustomerId`
- `ProductId`
- `IsApproved`
- `Title`
- `ReviewText`
- `Rating` (1-5)
- `HelpfulYesTotal`, `HelpfulNoTotal`
- `CreatedOnUtc`

### ProductReviewHelpfulness
**Purpose**: Track helpful votes on reviews

**Key Properties**:
- `ProductReviewId`
- `WasHelpful` - Yes/No
- `CustomerId`

### ProductTag
**Purpose**: Tags for product grouping and search

**Key Properties**:
- `Name` (required)

**Relationship**: Many-to-many with `Product` via junction table

### ProductSpecificationAttribute
**Purpose**: Product specifications (Technical specs)

**Key Properties**:
- `ProductId`
- `SpecificationAttributeOptionId`
- `AttributeTypeId` - Option, custom value, custom HTML
- `CustomValue` - For custom text/HTML
- `AllowFiltering` - Use in product filtering
- `ShowOnProductPage`
- `DisplayOrder`

### SpecificationAttribute
**Purpose**: Specification definitions

**Key Properties**:
- `Name` (required)
- `DisplayOrder`

### SpecificationAttributeOption
**Purpose**: Options for specifications

**Key Properties**:
- `SpecificationAttributeId`
- `Name` (required)
- `ColorSquaresRgb` - Color display
- `DisplayOrder`

### TierPrice
**Purpose**: Volume-based pricing

**Key Properties**:
- `ProductId`
- `StoreId` - Store-specific pricing (0 = all stores)
- `CustomerRoleId` - Role-specific pricing (0 = all customers)
- `Quantity` - Minimum quantity
- `Price` - Price for this tier

### RelatedProduct
**Purpose**: Related product suggestions

**Key Properties**:
- `ProductId1` - Main product
- `ProductId2` - Related product
- `DisplayOrder`

### CrossSellProduct
**Purpose**: Cross-sell suggestions (shown in cart)

**Key Properties**:
- `ProductId1` - Main product
- `ProductId2` - Cross-sell product

### BackInStockSubscription
**Purpose**: Customer notifications when product back in stock

**Key Properties**:
- `StoreId`
- `ProductId`
- `CustomerId`
- `CreatedOnUtc`

## Customer Domain

**Location**: `Nop.Core.Domain.Customers`

### Customer
**Purpose**: System users (customers, admins, guests)

**Key Properties**:
- `CustomerGuid` - Unique identifier (Guid)
- `Username` - Login username
- `Email` - Email address
- `EmailToRevalidate` - Email change verification
- `AdminComment` - Internal notes

**Security**:
- `FailedLoginAttempts` - Failed login count
- `CannotLoginUntilDateUtc` - Lockout until
- `RequireReLogin` - Force re-authentication

**Status**:
- `Active` - Account active flag
- `Deleted` - Soft delete
- `IsSystemAccount` - System account flag
- `SystemName` - System account name

**Business Associations**:
- `AffiliateId` - Affiliate association
- `VendorId` - Vendor account

**Tax**:
- `IsTaxExempt` - Tax exemption

**Tracking**:
- `LastIpAddress`
- `CreatedOnUtc`
- `LastLoginDateUtc`
- `LastActivityDateUtc`
- `RegisteredInStoreId` - Registration store

**Performance Optimization**:
- `HasShoppingCartItems` - Avoid loading cart if empty

**Navigation Properties**:
- `CustomerRoles` - Security roles
- `ShoppingCartItems` - Cart and wishlist
- `ExternalAuthenticationRecords` - OAuth logins
- `ReturnRequests` - Return requests
- `Addresses` - Saved addresses
- `BillingAddress` - Default billing
- `ShippingAddress` - Default shipping

### CustomerPassword
**Purpose**: Password history (for password reuse prevention)

**Key Properties**:
- `CustomerId`
- `Password` - Hashed password
- `PasswordFormatId` - Hash algorithm
- `PasswordSalt`
- `CreatedOnUtc`

### CustomerRole
**Purpose**: Security roles and permissions

**Key Properties**:
- `Name` (required)
- `FreeShipping` - Role gets free shipping
- `TaxExempt` - Role is tax exempt
- `Active`
- `IsSystemRole` - System role (cannot delete)
- `SystemName` - System role name
- `PurchasedWithProductId` - Role assigned with product purchase

**Standard Roles**:
- Administrators
- Registered
- Guests
- ForumModerators
- Vendors

### CustomerAttribute
**Purpose**: Custom customer attributes

**Key Properties**:
- `Name` (required)
- `IsRequired`
- `AttributeControlTypeId` - Control type
- `DisplayOrder`

### CustomerAttributeValue
**Purpose**: Values for customer attributes

**Key Properties**:
- `CustomerAttributeId`
- `Name` (required)
- `IsPreSelected`
- `DisplayOrder`

### ExternalAuthenticationRecord
**Purpose**: OAuth/OpenID authentication records

**Key Properties**:
- `CustomerId`
- `Email`
- `ExternalIdentifier` - Provider identifier
- `ExternalDisplayIdentifier` - Display name
- `OAuthToken`, `OAuthAccessToken`
- `ProviderSystemName` - Provider (Facebook, Google, etc.)

### RewardPointsHistory
**Purpose**: Customer loyalty points

**Key Properties**:
- `CustomerId`
- `StoreId`
- `Points` - Points earned/spent
- `PointsBalance` - Balance after transaction
- `UsedAmount` - Amount redeemed
- `Message` - Transaction description
- `UsedWithOrder` - Associated order
- `CreatedOnUtc`

## Order Domain

**Location**: `Nop.Core.Domain.Orders`

### Order
**Purpose**: Customer orders

**Key Properties**:

**Identification**:
- `OrderGuid` - Unique identifier
- `CustomOrderNumber` - Display order number
- `StoreId`

**Customer Information**:
- `CustomerId`
- `BillingAddressId`
- `ShippingAddressId`
- `CustomerLanguageId`
- `CustomerTaxDisplayTypeId`
- `CustomerIp`
- `AffiliateId`
- `VendorId` - For vendor orders

**Order Status**:
- `OrderStatusId` - Pending, Processing, Complete, Cancelled
- `ShippingStatusId` - Not yet shipped, Partially shipped, Shipped, Delivered
- `PaymentStatusId` - Pending, Authorized, Paid, Refunded, Voided

**Financial Details**:
- `OrderSubtotalInclTax`, `OrderSubtotalExclTax`
- `OrderSubTotalDiscountInclTax`, `OrderSubTotalDiscountExclTax`
- `OrderShippingInclTax`, `OrderShippingExclTax`
- `PaymentMethodAdditionalFeeInclTax`, `PaymentMethodAdditionalFeeExclTax`
- `TaxRates` - Serialized tax breakdown
- `OrderTax`
- `OrderDiscount`
- `OrderTotal`
- `RefundedAmount`
- `CurrencyRate`

**Payment Information**:
- `CheckoutAttributeDescription`
- `CheckoutAttributesXml`
- `CustomerCurrencyCode`
- `OrderWeight`
- `PaymentMethodSystemName`
- `AuthorizationTransactionId`
- `AuthorizationTransactionCode`
- `AuthorizationTransactionResult`
- `CaptureTransactionId`
- `CaptureTransactionResult`
- `SubscriptionTransactionId`
- `PaidDateUtc`

**Shipping Information**:
- `ShippingMethod`
- `ShippingRateComputationMethodSystemName`
- `CustomValuesXml`
- `PickUpInStore` - Pickup flag
- `PickupAddress` - Pickup location

**Reward Points**:
- `RewardPointsRemaining`
- `RewardPointsWereAdded`

**Dates**:
- `CreatedOnUtc`
- `UpdatedOnUtc`

**Flags**:
- `Deleted`

**Calculated Properties**:
- `OrderStatus`, `PaymentStatus`, `ShippingStatus` (enum conversions)

**Navigation Properties**:
- `OrderItems` - Line items
- `OrderNotes` - Notes and status changes
- `Shipments` - Shipment records
- `GiftCardUsageHistory` - Gift cards used

### OrderItem
**Purpose**: Order line items

**Key Properties**:
- `OrderId`
- `ProductId`
- `Quantity`
- `UnitPriceInclTax`, `UnitPriceExclTax`
- `PriceInclTax`, `PriceExclTax` - Extended price
- `DiscountAmountInclTax`, `DiscountAmountExclTax`
- `OriginalProductCost` - Cost at order time
- `AttributeDescription` - Selected attributes (display)
- `AttributesXml` - Serialized attributes
- `DownloadCount` - For downloadable products
- `IsDownloadActivated`
- `LicenseDownloadId`
- `ItemWeight`
- `RentalStartDateUtc`, `RentalEndDateUtc` - For rentals

### OrderNote
**Purpose**: Order comments and status history

**Key Properties**:
- `OrderId`
- `Note` - Comment text
- `DisplayToCustomer` - Visibility
- `CreatedOnUtc`

### ShoppingCartItem
**Purpose**: Cart and wishlist items

**Key Properties**:
- `StoreId`
- `ShoppingCartTypeId` - Cart or wishlist
- `CustomerId`
- `ProductId`
- `AttributesXml` - Selected attributes
- `CustomerEnteredPrice` - For customer-price products
- `Quantity`
- `RentalStartDateUtc`, `RentalEndDateUtc`
- `CreatedOnUtc`
- `UpdatedOnUtc`

**Calculated Property**:
- `ShoppingCartType` (enum conversion)

### CheckoutAttribute
**Purpose**: Custom checkout fields (gift message, special instructions)

**Key Properties**:
- `Name` (required)
- `TextPrompt`
- `IsRequired`
- `ShippableProductRequired` - Only show if shipping needed
- `IsTaxExempt`
- `TaxCategoryId`
- `AttributeControlTypeId`
- `DisplayOrder`
- `LimitedToStores`
- `ValidationMinLength`, `ValidationMaxLength`
- `ValidationFileAllowedExtensions`, `ValidationFileMaximumSize`
- `DefaultValue`
- `ConditionAttributeXml`

### CheckoutAttributeValue
**Purpose**: Values for checkout attributes

**Key Properties**:
- `CheckoutAttributeId`
- `Name` (required)
- `ColorSquaresRgb`
- `PriceAdjustment`
- `WeightAdjustment`
- `IsPreSelected`
- `DisplayOrder`

### GiftCard
**Purpose**: Gift card products

**Key Properties**:
- `GiftCardTypeId` - Virtual, Physical
- `PurchasedWithOrderItemId` - Source order
- `Amount`
- `IsGiftCardActivated`
- `GiftCardCouponCode` - Redemption code
- `RecipientName`, `RecipientEmail`
- `SenderName`, `SenderEmail`
- `Message`
- `IsRecipientNotified`
- `CreatedOnUtc`

### GiftCardUsageHistory
**Purpose**: Gift card redemptions

**Key Properties**:
- `GiftCardId`
- `UsedWithOrderId`
- `UsedValue`
- `CreatedOnUtc`

### ReturnRequest
**Purpose**: Product return requests

**Key Properties**:
- `StoreId`
- `OrderItemId`
- `CustomerId`
- `Quantity`
- `ReasonForReturn`
- `RequestedAction`
- `CustomerComments`
- `StaffNotes`
- `ReturnRequestStatusId` - Pending, Received, ItemsRepaired, ItemsRefunded, etc.
- `CreatedOnUtc`
- `UpdatedOnUtc`

**Calculated Property**:
- `ReturnRequestStatus` (enum conversion)

### ReturnRequestAction
**Purpose**: Predefined return actions

**Key Properties**:
- `Name` (required)
- `DisplayOrder`

### ReturnRequestReason
**Purpose**: Predefined return reasons

**Key Properties**:
- `Name` (required)
- `DisplayOrder`

### RecurringPayment
**Purpose**: Subscription/recurring payment tracking

**Key Properties**:
- `CycleLength`
- `CyclePeriodId` - Days, weeks, months, years
- `TotalCycles`
- `StartDateUtc`
- `IsActive`
- `Deleted`
- `InitialOrderId` - First order
- `CreatedOnUtc`
- `LastPaymentDate`
- `NextPaymentDate`

**Calculated Property**:
- `CyclePeriod` (enum conversion)

### RecurringPaymentHistory
**Purpose**: Recurring payment transactions

**Key Properties**:
- `RecurringPaymentId`
- `OrderId` - Created order
- `CreatedOnUtc`

## Content Domain

### Blog Entities
**Location**: `Nop.Core.Domain.Blogs`

#### BlogPost
- `LanguageId`
- `Title` (required)
- `Body` (required)
- `BodyOverview`
- `AllowComments`
- `CommentCount` - Denormalized
- `Tags` - Comma-separated
- `StartDateUtc`, `EndDateUtc`
- `MetaKeywords`, `MetaDescription`, `MetaTitle`
- `LimitedToStores`
- `CreatedOnUtc`

#### BlogComment
- `CustomerId`
- `CommentText`
- `BlogPostId`
- `StoreId`
- `CreatedOnUtc`

### News Entities
**Location**: `Nop.Core.Domain.News`

#### NewsItem
- `LanguageId`
- `Title` (required)
- `Short`, `Full` - Content
- `Published`
- `StartDateUtc`, `EndDateUtc`
- `AllowComments`
- `CommentCount`
- `MetaKeywords`, `MetaDescription`, `MetaTitle`
- `LimitedToStores`
- `CreatedOnUtc`

#### NewsComment
- `CustomerId`
- `CommentTitle`, `CommentText`
- `NewsItemId`
- `StoreId`
- `CreatedOnUtc`

### Forum Entities
**Location**: `Nop.Core.Domain.Forums`

#### ForumGroup
- `Name` (required)
- `Description`
- `DisplayOrder`
- `CreatedOnUtc`, `UpdatedOnUtc`

#### Forum
- `ForumGroupId`
- `Name` (required)
- `Description`
- `NumTopics`, `NumPosts` - Denormalized counts
- `LastTopicId`, `LastPostId`, `LastPostCustomerId`, `LastPostTime`
- `DisplayOrder`
- `CreatedOnUtc`, `UpdatedOnUtc`

#### ForumTopic
- `ForumId`
- `CustomerId`
- `TopicTypeId` - Normal, Sticky, Announcement
- `Subject` (required)
- `NumPosts` - Denormalized
- `Views`
- `LastPostId`, `LastPostCustomerId`, `LastPostTime`
- `Published`
- `CreatedOnUtc`, `UpdatedOnUtc`

#### ForumPost
- `TopicId`
- `CustomerId`
- `Text` (required)
- `IPAddress`
- `Published`
- `VoteCount` - Post voting
- `CreatedOnUtc`, `UpdatedOnUtc`

#### ForumPostVote
- `ForumPostId`
- `CustomerId`
- `Vote` - Up/down
- `CreatedOnUtc`

#### ForumSubscription
- `SubscriptionGuid`
- `CustomerId`
- `ForumId`, `TopicId` - Forum or topic subscription
- `CreatedOnUtc`

#### PrivateMessage
- `StoreId`
- `FromCustomerId`, `ToCustomerId`
- `Subject` (required, max 450)
- `Text` (required)
- `IsRead`, `IsDeletedByAuthor`, `IsDeletedByRecipient`
- `CreatedOnUtc`

### Poll Entities
**Location**: `Nop.Core.Domain.Polls`

#### Poll
- `LanguageId`
- `Name` (required)
- `SystemKeyword`
- `Published`
- `ShowOnHomePage`
- `AllowGuestsToVote`
- `DisplayOrder`
- `StartDateUtc`, `EndDateUtc`
- `LimitedToStores`

#### PollAnswer
- `PollId`
- `Name` (required)
- `NumberOfVotes` - Denormalized
- `DisplayOrder`

#### PollVotingRecord
- `PollAnswerId`
- `CustomerId`
- `CreatedOnUtc`

### Topic (Content Pages)
**Location**: `Nop.Core.Domain.Topics`

#### Topic
- `SystemName` - Identifier for system pages
- `IncludeInSitemap`
- `IncludeInTopMenu`, `IncludeInFooterColumn1`, `IncludeInFooterColumn2`, `IncludeInFooterColumn3`
- `DisplayOrder`
- `AccessibleWhenStoreClosed`
- `IsPasswordProtected`, `Password`
- `Title`
- `Body`
- `Published`
- `TopicTemplateId`
- `MetaKeywords`, `MetaDescription`, `MetaTitle`
- `SubjectToAcl`, `LimitedToStores`

**Interfaces**: `ILocalizedEntity`, `ISlugSupported`, `IAclSupported`, `IStoreMappingSupported`

## Configuration and Settings

### Common Entities

#### Address
**Location**: `Nop.Core.Domain.Common`

- `FirstName`, `LastName`
- `Email`, `Company`
- `CountryId`, `StateProvinceId`
- `City`, `Address1`, `Address2`
- `ZipPostalCode`
- `PhoneNumber`, `FaxNumber`
- `CustomAttributes` - Serialized custom attributes
- `CreatedOnUtc`

#### GenericAttribute
**Purpose**: Extension properties for entities

- `EntityId`, `KeyGroup` - Entity reference
- `Key` - Attribute name
- `Value` - Attribute value (text)
- `StoreId` - Store-specific attributes

**Common Uses**:
- Customer preferences
- Admin settings
- Temporary data

### Directory Entities
**Location**: `Nop.Core.Domain.Directory`

#### Country
- `Name` (required, max 100)
- `AllowsBilling`, `AllowsShipping`
- `TwoLetterIsoCode` (required, max 2)
- `ThreeLetterIsoCode` (required, max 3)
- `NumericIsoCode`
- `SubjectToVat` - EU VAT
- `Published`, `DisplayOrder`
- `LimitedToStores`

#### StateProvince
- `CountryId`
- `Name` (required, max 100)
- `Abbreviation` (max 100)
- `Published`, `DisplayOrder`

#### Currency
- `Name` (required, max 50)
- `CurrencyCode` (required, max 5) - USD, EUR, GBP
- `Rate` - Exchange rate
- `DisplayLocale` - Culture for formatting
- `CustomFormatting` - Custom format string
- `LimitedToStores`
- `Published`, `DisplayOrder`
- `CreatedOnUtc`, `UpdatedOnUtc`

#### MeasureWeight, MeasureDimension
- `Name` (required, max 100)
- `SystemKeyword` (required, max 100)
- `Ratio` - Conversion ratio to base unit
- `DisplayOrder`

### Discounts
**Location**: `Nop.Core.Domain.Discounts`

#### Discount
- `Name` (required)
- `DiscountTypeId` - Assigned to order total, order subtotal, product, category, manufacturer
- `UsePercentage` - Percentage vs. fixed amount
- `DiscountPercentage`, `DiscountAmount`
- `MaximumDiscountAmount` - Cap for percentage discounts
- `StartDateUtc`, `EndDateUtc`
- `RequiresCouponCode`
- `CouponCode` (max 100)
- `IsCumulative` - Can combine with other discounts
- `DiscountLimitationId` - Unlimited, n times total, n times per customer
- `LimitationTimes`
- `MaximumDiscountedQuantity` - For product discounts
- `AppliedToSubCategories` - For category discounts
- `LimitedToStores`

**Navigation Properties**:
- `DiscountRequirements` - Complex discount rules
- `AppliedToProducts`, `AppliedToCategories`, `AppliedToManufacturers` - Assignments

#### DiscountRequirement
**Purpose**: Complex discount conditions (extensible via plugins)

- `DiscountId`
- `DiscountRequirementRuleSystemName` - Plugin identifier
- `InteractionTypeId` - And/Or logic
- `ParentId` - Hierarchical conditions

#### DiscountUsageHistory
- `DiscountId`
- `OrderId`
- `CreatedOnUtc`

### Shipping Entities
**Location**: `Nop.Core.Domain.Shipping`

#### ShippingMethod
- `Name` (required, max 400)
- `Description`
- `DisplayOrder`

#### Warehouse
- `Name` (required, max 400)
- `AdminComment`
- `AddressId` - Warehouse location

#### DeliveryDate
- `Name` (required, max 400)
- `DisplayOrder`

#### ProductAvailabilityRange
- `Name` (required, max 400)
- `DisplayOrder`

#### Shipment
- `OrderId`
- `TrackingNumber`
- `TotalWeight`
- `ShippedDateUtc`, `DeliveryDateUtc`
- `AdminComment`
- `CreatedOnUtc`

#### ShipmentItem
- `ShipmentId`
- `OrderItemId`
- `Quantity`
- `WarehouseId`

### Tax Entities
**Location**: `Nop.Core.Domain.Tax`

#### TaxCategory
- `Name` (required, max 400)
- `DisplayOrder`

### Vendor Entities
**Location**: `Nop.Core.Domain.Vendors`

#### Vendor
- `Name` (required, max 400)
- `Email` (required, max 400)
- `Description`
- `AdminComment`
- `PictureId` - Vendor logo
- `Active`, `Deleted`
- `DisplayOrder`
- `PageSize`, `AllowCustomersToSelectPageSize`, `PageSizeOptions`
- `MetaKeywords`, `MetaDescription`, `MetaTitle`

**Interfaces**: `ILocalizedEntity`, `ISlugSupported`

#### VendorNote
- `VendorId`
- `Note` (required)
- `CreatedOnUtc`

### Media Entities
**Location**: `Nop.Core.Domain.Media`

#### Picture
- `PictureBinary` - Image data
- `MimeType` (required, max 40)
- `SeoFilename` (max 300)
- `AltAttribute` (max 100)
- `TitleAttribute` (max 100)
- `IsNew` - Lazy loading flag
- `UpdatedOnUtc`

#### Download
- `DownloadGuid`
- `UseDownloadUrl` - URL vs. binary
- `DownloadUrl`
- `DownloadBinary`
- `ContentType` (required)
- `Filename` (max 100)
- `Extension` (max 20)
- `IsNew` - Lazy loading flag

### Message Entities
**Location**: `Nop.Core.Domain.Messages`

#### EmailAccount
- `Email` (required, max 255)
- `DisplayName` (max 255)
- `Host` (required, max 255) - SMTP host
- `Port`
- `Username`, `Password` - SMTP credentials
- `EnableSsl`
- `UseDefaultCredentials`

#### MessageTemplate
- `Name` (required, max 200) - Template identifier
- `BccEmailAddresses` (max 200) - BCC recipients
- `Subject` (max 1000)
- `Body` - HTML body with tokens
- `IsActive`
- `DelayPeriodId` - Immediate or delayed sending
- `DelayBeforeSend` - Delay in hours
- `AttachedDownloadId` - Attachment
- `EmailAccountId`
- `LimitedToStores`

#### QueuedEmail
- `Priority` - Low, Medium, High
- `From` (required, max 500), `FromName` (max 500)
- `To` (required, max 500), `ToName` (max 500)
- `ReplyTo` (max 500), `ReplyToName` (max 500)
- `CC` (max 500), `Bcc` (max 500)
- `Subject` (max 1000)
- `Body`
- `AttachmentFilePath`, `AttachmentFileName`
- `AttachedDownloadId`
- `CreatedOnUtc`
- `DontSendBeforeDateUtc`
- `SentTries`
- `SentOnUtc`
- `EmailAccountId`

#### NewsLetterSubscription
- `NewsLetterSubscriptionGuid`
- `Email` (required, max 255)
- `Active`
- `StoreId`
- `CreatedOnUtc`

#### Campaign
- `Name` (required)
- `Subject` (required)
- `Body` (required)
- `StoreId`
- `CustomerRoleId` - Target role
- `CreatedOnUtc`
- `DontSendBeforeDateUtc`

### Localization Entities
**Location**: `Nop.Core.Domain.Localization`

#### Language
- `Name` (required, max 100)
- `LanguageCulture` (required, max 20) - en-US, de-DE
- `UniqueSeoCode` (max 2) - en, de
- `FlagImageFileName` (max 50)
- `Rtl` - Right-to-left flag
- `LimitedToStores`
- `DefaultCurrencyId`
- `Published`, `DisplayOrder`

#### LocaleStringResource
- `LanguageId`
- `ResourceName` (required, max 200)
- `ResourceValue` (required)

#### LocalizedProperty
**Purpose**: Entity property translations

- `EntityId`, `LocaleKeyGroup` - Entity reference
- `LocaleKey` - Property name
- `LocaleValue` - Translated value
- `LanguageId`

### SEO Entities
**Location**: `Nop.Core.Domain.Seo`

#### UrlRecord
- `EntityId`, `EntityName` - Entity reference
- `Slug` (required, max 400) - SEO-friendly URL
- `IsActive` - One active slug per entity per language
- `LanguageId`

### Security Entities
**Location**: `Nop.Core.Domain.Security`

#### PermissionRecord
- `Name` (required)
- `SystemName` (required) - Identifier
- `Category` (required) - Grouping

#### AclRecord
- `EntityId`, `EntityName` - Entity reference
- `CustomerRoleId` - Allowed role

### Store Entities
**Location**: `Nop.Core.Domain.Stores`

#### Store
- `Name` (required, max 400)
- `Url` (required, max 400)
- `SslEnabled`, `SecureUrl` (max 400)
- `Hosts` - Comma-separated host names
- `DefaultLanguageId`
- `DisplayOrder`
- `CompanyName`, `CompanyAddress`, `CompanyPhoneNumber`
- `CompanyVat` - VAT number

#### StoreMapping
- `EntityId`, `EntityName` - Entity reference
- `StoreId`

### Task Entities
**Location**: `Nop.Core.Domain.Tasks`

#### ScheduleTask
- `Name` (required)
- `Seconds` - Run interval
- `Type` (required) - Task class type
- `Enabled`
- `StopOnError`
- `LastStartUtc`, `LastEndUtc`, `LastSuccessUtc`

### Configuration Entities
**Location**: `Nop.Core.Domain.Configuration`

#### Setting
- `Name` (required, max 200)
- `Value` (required, max 2000)
- `StoreId` - Store-specific settings (0 = all stores)

### Logging Entities
**Location**: `Nop.Core.Domain.Logging`

#### Log
- `LogLevelId` - Debug, Info, Warning, Error, Fatal
- `ShortMessage` (required)
- `FullMessage`
- `IpAddress` (max 200)
- `CustomerId` - Associated customer
- `PageUrl`
- `ReferrerUrl`
- `CreatedOnUtc`

#### ActivityLog
- `ActivityLogTypeId`
- `EntityId`, `EntityName` - Related entity
- `Comment` (required)
- `CustomerId`
- `IpAddress` (max 200)
- `CreatedOnUtc`

#### ActivityLogType
- `SystemKeyword` (required, max 100)
- `Name` (required, max 200)
- `Enabled`

## Entity Relationships

### Key Relationships

**Product Relationships**:
- Product → ProductCategory (many-to-many via ProductCategory)
- Product → ProductManufacturer (many-to-many via ProductManufacturer)
- Product → ProductPicture (one-to-many)
- Product → ProductReview (one-to-many)
- Product → ProductTag (many-to-many)
- Product → ProductAttributeMapping (one-to-many)
- Product → TierPrice (one-to-many)
- Product → Discount (many-to-many)

**Customer Relationships**:
- Customer → CustomerRole (many-to-many)
- Customer → Address (many-to-many)
- Customer → ShoppingCartItem (one-to-many)
- Customer → Order (one-to-many)
- Customer → RewardPointsHistory (one-to-many)

**Order Relationships**:
- Order → OrderItem (one-to-many)
- Order → OrderNote (one-to-many)
- Order → Shipment (one-to-many)
- OrderItem → Product (many-to-one)
- OrderItem → ShipmentItem (one-to-many)

**Multi-Store/Localization**:
- Many entities → Store (many-to-many via StoreMapping)
- Many entities → Language (many-to-many via LocalizedProperty)

**SEO**:
- Many entities → UrlRecord (one-to-many, one active per language)

**ACL**:
- Many entities → CustomerRole (many-to-many via AclRecord)

## Summary

The nopCommerce data model demonstrates:

1. **Rich Domain Model**: Comprehensive entities covering all e-commerce scenarios
2. **Entity Inheritance**: Common base entity with proper equality
3. **Soft Delete**: Non-destructive deletion pattern
4. **Audit Trails**: Created/Updated timestamps
5. **Multi-Store Support**: Store-specific data via StoreMapping
6. **Localization**: Multi-language via LocalizedProperty
7. **SEO Support**: SEO-friendly URLs via UrlRecord
8. **Access Control**: Role-based ACL via AclRecord
9. **Performance Optimization**: Denormalized counts, optimization flags
10. **Extensibility**: GenericAttribute for custom properties

The model supports complex business scenarios while maintaining clean separation of concerns and following domain-driven design principles.

---

**Related Documentation:**
- [Program Structure](program-structure.md)
- [Interfaces Documentation](interfaces.md)
- [Dependencies](../architecture/dependencies.md)
