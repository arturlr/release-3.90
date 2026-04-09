# nopCommerce Error Handling Documentation

## Overview

This document describes error handling, exception management, validation patterns, and recovery strategies throughout the nopCommerce platform. The system employs multiple layers of error handling to ensure robustness and provide clear user feedback.

## Table of Contents
- [Exception Handling Architecture](#exception-handling-architecture)
- [Validation Patterns](#validation-patterns)
- [Error Reporting and Logging](#error-reporting-and-logging)
- [User-Facing Error Messages](#user-facing-error-messages)
- [Payment Error Handling](#payment-error-handling)
- [Database Error Handling](#database-error-handling)
- [API and Plugin Error Handling](#api-and-plugin-error-handling)
- [Recovery Strategies](#recovery-strategies)

---

## Exception Handling Architecture

### Global Exception Handling

**ASP.NET MVC Error Handling**:
```
Request → Controller Action
    │
    ├─> Success → Return View/JSON
    │
    └─> Exception Thrown
        │
        ├─> Caught by HandleErrorAttribute (MVC filter)
        │   ├─> Log exception to system log
        │   ├─> Check if AJAX request
        │   │   ├─> YES → Return JSON error response
        │   │   └─> NO → Redirect to Error view
        │   └─> Set HTTP status code (500, 404, etc.)
        │
        ├─> OR: Caught by Application_Error (Global.asax)
        │   ├─> Log to ILogger
        │   ├─> Send admin notification (if critical)
        │   └─> Display generic error page
        │
        └─> Unhandled → ASP.NET Yellow Screen of Death (debug mode)
                     → Custom error page (production mode)
```

**Location**: 
- `Nop.Web.Framework.Mvc.Filters.HandleErrorAttribute`
- `Global.asax.cs Application_Error` method

### Exception Types and Handling

**Custom Exceptions**:
```csharp
// nopCommerce custom exceptions

NopException
├─> Base exception for all nopCommerce exceptions
├─> Caught and logged at application boundary
└─> Contains localized message for user display

ValidationException
├─> Thrown when business rule validation fails
├─> Contains list of validation errors
├─> Returns 400 Bad Request in API contexts
└─> Example: "Product name is required"

ApplicationException (framework)
├─> Generic application errors
└─> Used for unexpected business logic errors

SecurityException (framework)
├─> Access denied scenarios
├─> Returns 403 Forbidden
└─> Example: Unauthorized access attempt

EntityNotFoundException
├─> Requested resource not found
├─> Returns 404 Not Found
└─> Example: Product ID 12345 doesn't exist
```

**Exception Handling Patterns**:

**Pattern 1: Try-Catch with Logging**
```csharp
try
{
    // Perform operation
    var result = service.ProcessOrder(order);
    return result;
}
catch (Exception ex)
{
    // Log exception with context
    _logger.Error($"Order processing failed for Order {order.Id}", ex);
    
    // Return user-friendly error
    return Error("Unable to process your order. Please try again.");
}
```

**Pattern 2: Validation Before Operation**
```csharp
// Validate first, throw specific exceptions
if (string.IsNullOrEmpty(product.Name))
    throw new ValidationException("Product name is required");

if (product.Price < 0)
    throw new ValidationException("Product price cannot be negative");

// Proceed with operation only if valid
_productRepository.Insert(product);
```

**Pattern 3: Safe Execution with Fallback**
```csharp
// Attempt operation with graceful degradation
try
{
    price = _priceCalculationService.GetFinalPrice(product, customer);
}
catch (Exception ex)
{
    _logger.Warning("Price calculation failed, using base price", ex);
    price = product.Price; // Fallback to base price
}
```

---

## Validation Patterns

### Input Validation

**Client-Side Validation** (JavaScript/jQuery):
```
Form Submit → jQuery Validation
    │
    ├─> Required fields present?
    ├─> Email format valid?
    ├─> Number ranges correct?
    ├─> Custom validation rules passed?
    │
    ├─> All Valid → Submit to server
    └─> Validation Failed → Display inline errors, prevent submit
```

**Server-Side Validation** (Model State):
```
POST Request → Model Binding
    │
    ├─> MVC validates against:
    │   ├─> Data annotations ([Required], [EmailAddress], [Range])
    │   ├─> Custom validation attributes
    │   └─> IValidatableObject.Validate()
    │
    ├─> ModelState.IsValid?
    │   ├─> YES → Proceed with action
    │   └─> NO → Return view with errors
    │       └─> ValidationSummary displays all errors
    │       └─> ValidationMessage shows per-field errors
    │
    └─> Additional Business Validation:
        └─> Service layer validates business rules
        └─> Returns list of errors if invalid
```

**Validation Implementation Locations**:
- `Nop.Web.Framework.Validators` - FluentValidation validators
- `Nop.Admin.Validators` - Admin-specific validation
- Model classes with DataAnnotations attributes

### Business Rule Validation

**Validation Result Pattern**:
```csharp
// Service returns validation result instead of throwing

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
}

// Usage in service:
public ValidationResult ValidateProduct(Product product)
{
    var result = new ValidationResult { Errors = new List<string>() };
    
    if (string.IsNullOrEmpty(product.Name))
        result.Errors.Add("Product name is required");
    
    if (product.Price < 0)
        result.Errors.Add("Price cannot be negative");
    
    if (product.StockQuantity < 0)
        result.Errors.Add("Stock quantity cannot be negative");
    
    result.IsValid = result.Errors.Count == 0;
    return result;
}

// Controller usage:
var validation = _productService.ValidateProduct(model);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
        ModelState.AddModelError("", error);
    
    return View(model);
}
```

**Validation at Different Layers**:

1. **Controller Layer**: Input format, required fields
2. **Service Layer**: Business rules, constraints, dependencies
3. **Repository Layer**: Database constraints, referential integrity
4. **Domain Layer**: Entity-level invariants

---

## Error Reporting and Logging

### Logging Infrastructure

**ILogger Interface** (Nop.Core.Infrastructure):
```csharp
public interface ILogger
{
    void Debug(string message, Exception exception = null);
    void Information(string message, Exception exception = null);
    void Warning(string message, Exception exception = null);
    void Error(string message, Exception exception = null);
    void Fatal(string message, Exception exception = null);
}
```

**Log Levels**:
- **Debug**: Detailed diagnostic information (development only)
- **Information**: General informational messages (e.g., "Order placed")
- **Warning**: Potentially harmful situations (e.g., "Cache miss")
- **Error**: Error events that might still allow application to continue
- **Fatal**: Very severe errors that may cause application termination

**Log Destinations**:
- **Database**: `Log` table (default)
- **File**: Text files in `App_Data/Logs/` (if configured)
- **Email**: Critical errors emailed to admin (if configured)
- **External**: Integration with ELK, Splunk, etc. (via plugins)

**Logging Best Practices**:
```csharp
// Good: Contextual information included
_logger.Error($"Payment processing failed. OrderId: {order.Id}, " +
              $"CustomerId: {customer.Id}, Amount: {order.Total}", exception);

// Good: Include customer information for support
_logger.Information($"User registration: Email={email}, IP={ipAddress}");

// Avoid: Logging sensitive data
_logger.Debug($"Password: {password}"); // NEVER DO THIS

// Avoid: Logging in tight loops (performance impact)
foreach (var product in millionsOfProducts)
{
    _logger.Debug($"Processing {product.Id}"); // Too verbose
}
```

### Error Notification

**Admin Notifications**:
```
Critical Error Occurs
    │
    ├─> Log to database/file
    │
    ├─> Check notification settings:
    │   └─> Should notify admin of errors?
    │       └─> YES → Send email to store owner
    │           ├─> Subject: "Error on {store name}"
    │           ├─> Body: Exception details, stack trace
    │           └─> Include: Timestamp, user info, request URL
    │
    └─> Display error page to user
        └─> Generic message (don't expose internals)
        └─> Error reference ID for support lookup
```

**Implementation**: `Nop.Services.Logging.DefaultLogger`

---

## User-Facing Error Messages

### Error Message Categories

**1. Validation Errors** (User can fix):
```
Examples:
- "Email address is required"
- "Password must be at least 6 characters"
- "Credit card number is invalid"
- "Product is out of stock"

Display:
- Inline next to form field (red text)
- Validation summary at top of form
- Prevent form submission until corrected
```

**2. Business Rule Errors** (User action required):
```
Examples:
- "Minimum order amount is $25"
- "This coupon code has expired"
- "Product cannot be shipped to your country"
- "Maximum quantity for this product is 10"

Display:
- Warning message box (yellow background)
- Clear explanation of requirement
- Suggest alternative action if possible
```

**3. System Errors** (Beyond user control):
```
Examples:
- "Payment gateway is temporarily unavailable"
- "Unable to calculate shipping rates"
- "An error occurred processing your request"
- "Service temporarily unavailable"

Display:
- Error message box (red background)
- Generic message (don't expose technical details)
- Provide error reference number
- Suggest retry or contact support
```

**4. Success Confirmations** (Positive feedback):
```
Examples:
- "Product added to cart"
- "Order placed successfully"
- "Settings saved"
- "Password changed"

Display:
- Success message box (green background)
- Brief, clear confirmation
- Auto-dismiss after few seconds (for non-critical)
```

### Localization of Errors

All error messages stored in resource files for localization:
```csharp
// Instead of hardcoded strings:
return Error("Product not found");

// Use localized resources:
return Error(_localizationService.GetResource("Products.ProductNotFound"));
```

**Resource File Structure**:
```
en-US:
  Products.ProductNotFound = "The product was not found"
  Checkout.MinimumOrder = "Minimum order amount is {0}"
  
es-ES:
  Products.ProductNotFound = "El producto no fue encontrado"
  Checkout.MinimumOrder = "El monto mínimo de pedido es {0}"
```

---

## Payment Error Handling

### Payment Processing Error Flow

```
Payment Processing Initiated
    │
    ├─> Pre-validation:
    │   ├─> Payment method active? → NO → Error: "Payment method unavailable"
    │   ├─> Card details valid? → NO → Error: "Invalid card information"
    │   └─> Amount valid? → NO → Error: "Invalid payment amount"
    │
    ├─> Call Payment Gateway:
    │   │
    │   └─> Gateway Response:
    │       │
    │       ├─> SUCCESS (Authorized/Paid):
    │       │   └─> Create order, send confirmation
    │       │
    │       ├─> DECLINED:
    │       │   ├─> Reason: Insufficient funds
    │       │   ├─> Reason: Invalid card
    │       │   ├─> Reason: Card expired
    │       │   ├─> Reason: Address mismatch
    │       │   └─> Display: Specific decline reason to customer
    │       │       └─> Allow retry with different payment method
    │       │
    │       ├─> FRAUD SUSPECTED:
    │       │   ├─> Log security incident
    │       │   ├─> Display: Generic "unable to process" message
    │       │   └─> Require manual review
    │       │
    │       ├─> GATEWAY ERROR:
    │       │   ├─> Log error details
    │       │   ├─> Display: "Payment service temporarily unavailable"
    │       │   └─> Suggest: Try again later or use different method
    │       │
    │       └─> TIMEOUT:
    │           ├─> Log timeout
    │           ├─> Display: "Payment processing took too long"
    │           ├─> Check order status (may have succeeded)
    │           └─> Prevent duplicate orders
    │
    └─> Exception Handling:
        ├─> Catch payment plugin exceptions
        ├─> Log full details for troubleshooting
        ├─> Don't expose gateway errors to customer
        └─> Display generic error with reference number
```

**Error Recovery**:
- Orders NOT created if payment fails
- Inventory NOT reduced if payment fails
- Customer can retry immediately
- Previous payment details NOT saved (PCI compliance)
- Order held in "Pending" if payment status unclear

**Implementation**: `Nop.Services.Payments.PaymentService`

---

## Database Error Handling

### Database Operation Errors

**Connection Errors**:
```
Database Operation → SqlException
    │
    ├─> Exception.Number = -1 (Connection timeout):
    │   ├─> Log: "Database connection timeout"
    │   ├─> Retry operation (up to 3 times with delay)
    │   └─> If all retries fail:
    │       └─> Display: "Service temporarily unavailable"
    │       └─> Queue operation for later (if applicable)
    │
    ├─> Exception.Number = -2 (Command timeout):
    │   ├─> Log: "Database command timeout for query: {sql}"
    │   ├─> Don't retry (likely expensive query)
    │   └─> Display: "Request took too long, please try again"
    │
    └─> Exception.Number = other:
        └─> Log full exception
        └─> Display generic error
```

**Constraint Violation Errors**:
```
Database Constraint Violation → SqlException
    │
    ├─> Foreign Key Violation:
    │   └─> Cannot delete entity with dependencies
    │       └─> Display: "Cannot delete. Item is in use."
    │       └─> Suggest: "Remove dependencies first"
    │
    ├─> Unique Constraint Violation:
    │   └─> Duplicate email, username, SKU, etc.
    │       └─> Display: "Email address already registered"
    │       └─> Suggest: "Use a different email"
    │
    ├─> Not Null Constraint:
    │   └─> Required field missing (shouldn't happen - validation failed)
    │       └─> Log: "Validation bypassed for {entity}"
    │       └─> Display: "Required information missing"
    │
    └─> Check Constraint:
        └─> Data violates business rule
        └─> Display: "Invalid data: {constraint message}"
```

**Concurrency Errors**:
```
Optimistic Concurrency Conflict → DbUpdateConcurrencyException
    │
    ├─> Entity modified by another user/process
    │
    ├─> Refresh entity from database
    │
    ├─> Apply conflict resolution strategy:
    │   ├─> Client Wins: Overwrite with user's changes
    │   ├─> Store Wins: Discard user's changes, show current
    │   └─> Merge: Combine changes if possible
    │
    └─> Display: "This item was modified. Please review and save again."
        └─> Show current values
        └─> Allow user to decide
```

**Implementation**: Entity Framework exception handling in `Nop.Data`

---

## API and Plugin Error Handling

### External API Error Handling

**HTTP API Call Error Handling**:
```
Call External API (Shipping, Payment, Tax, etc.)
    │
    ├─> HTTP Status Codes:
    │   │
    │   ├─> 200 OK: Parse response, handle data
    │   │
    │   ├─> 400 Bad Request:
    │   │   └─> Our request was invalid
    │   │   └─> Log request/response
    │   │   └─> Display: "Invalid request parameters"
    │   │
    │   ├─> 401/403 Unauthorized/Forbidden:
    │   │   └─> API credentials invalid/expired
    │   │   └─> Log: "API authentication failed"
    │   │   └─> Notify admin: "Update API credentials"
    │   │   └─> Display: "Service unavailable"
    │   │
    │   ├─> 404 Not Found:
    │   │   └─> API endpoint changed
    │   │   └─> Log: "API endpoint not found"
    │   │   └─> Check for API version update
    │   │
    │   ├─> 429 Too Many Requests:
    │   │   └─> Rate limit exceeded
    │   │   └─> Implement retry with backoff
    │   │   └─> Cache results to reduce calls
    │   │
    │   ├─> 500/502/503 Server Errors:
    │   │   └─> External service down
    │   │   └─> Retry with exponential backoff
    │   │   └─> Fall back to default behavior
    │   │   └─> Display: "Service temporarily unavailable"
    │   │
    │   └─> Timeout:
    │       └─> Network/API slow or unresponsive
    │       └─> Set reasonable timeout (e.g., 30 seconds)
    │       └─> Don't wait indefinitely
    │
    ├─> Parse Response:
    │   ├─> Invalid JSON/XML → Parse error
    │   │   └─> Log response body
    │   │   └─> Possibly API changed format
    │   │
    │   └─> Unexpected data structure
    │       └─> Handle gracefully
    │       └─> Use defaults where possible
    │
    └─> Exception Handling:
        ├─> WebException: Network issues
        ├─> JsonException: Invalid response format
        └─> General Exception: Unexpected errors
            └─> All logged with context
            └─> Display generic error to user
```

**Fallback Strategies**:
- **Shipping Rates**: Use fixed backup rates if API fails
- **Tax Calculation**: Use default tax rate if provider fails
- **Payment Gateway**: Offer alternative payment method
- **Search Service**: Fall back to database search

### Plugin Error Isolation

**Plugin Execution Safety**:
```
Execute Plugin Method
    │
    ├─> Wrap in try-catch:
    │   │
    │   ├─> Plugin executes successfully → Return result
    │   │
    │   └─> Plugin throws exception:
    │       ├─> Catch and log exception
    │       ├─> Log plugin identity and method
    │       ├─> Don't propagate to main application
    │       ├─> Return null/default/empty result
    │       └─> Mark plugin as "faulted" (if repeated failures)
    │
    ├─> Plugin Timeout Protection:
    │   └─> If plugin takes > X seconds:
    │       └─> Log: "Plugin timeout: {pluginName}"
    │       └─> Cancel plugin execution
    │       └─> Continue with default behavior
    │
    └─> Invalid Plugin Behavior:
        └─> Plugin returns invalid data:
            └─> Validate plugin output
            └─> Log validation errors
            └─> Use default values
            └─> Notify admin of misbehaving plugin
```

**Plugin Loading Errors**:
```
Load Plugin on Startup
    │
    ├─> Plugin DLL missing/corrupted:
    │   └─> Log error
    │   └─> Skip plugin
    │   └─> Application continues without plugin
    │
    ├─> Plugin dependency missing:
    │   └─> Log missing dependencies
    │   └─> Don't load plugin
    │   └─> Notify admin
    │
    ├─> Plugin configuration invalid:
    │   └─> Load plugin but mark as "not configured"
    │   └─> Prompt admin to configure
    │
    └─> Plugin throws in constructor/initialization:
        └─> Catch exception
        └─> Log and skip plugin
        └─> Don't fail application startup
```

---

## Recovery Strategies

### Automatic Recovery

**1. Retry with Exponential Backoff**:
```csharp
public T ExecuteWithRetry<T>(Func<T> operation, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return operation();
        }
        catch (TransientException ex)
        {
            if (attempt == maxRetries)
                throw;
            
            int delayMs = (int)Math.Pow(2, attempt) * 1000; // 2s, 4s, 8s
            _logger.Warning($"Attempt {attempt} failed, retrying in {delayMs}ms");
            Thread.Sleep(delayMs);
        }
    }
}
```

**2. Circuit Breaker Pattern**:
```
Service Call → Check Circuit State
    │
    ├─> CLOSED (normal):
    │   ├─> Execute call
    │   ├─> Success → Reset failure count
    │   └─> Failure → Increment failure count
    │       └─> Failures > threshold → Open circuit
    │
    ├─> OPEN (failing):
    │   ├─> Don't attempt call
    │   ├─> Return cached/default result immediately
    │   └─> After timeout → Move to HALF-OPEN
    │
    └─> HALF-OPEN (testing):
        ├─> Allow single test call
        ├─> Success → Close circuit (resume normal)
        └─> Failure → Open circuit again
```

**3. Graceful Degradation**:
```
Feature Fails → Provide Reduced Functionality
    │
    Examples:
    ├─> Search service down → Use basic database search
    ├─> Image CDN down → Serve images from local storage
    ├─> Recommendation engine down → Show popular products
    ├─> Real-time shipping rates fail → Use fixed rates
    └─> External auth provider down → Use local login only
```

### Manual Recovery (Admin Actions)

**1. Clear Cache**:
- Admin can clear all caches when data seems stale
- Resolves cached error states
- Forces refresh of external data

**2. Restart Application**:
- Recycle application pool
- Reload all plugins and configurations
- Reset in-memory state

**3. Database Repair**:
- Identify and fix constraint violations
- Rebuild indexes
- Update statistics for performance

**4. View Logs**:
- Admin panel provides log viewing
- Filter by severity, date, logger
- Download logs for support analysis

**5. Disable Faulty Plugins**:
- Admin can disable problematic plugins
- Application continues without failed plugin
- Re-enable after plugin update

---

## Best Practices Summary

### Error Handling Do's:
1. ✅ Log all errors with sufficient context
2. ✅ Display user-friendly error messages
3. ✅ Validate input at multiple layers
4. ✅ Use specific exception types
5. ✅ Implement retry logic for transient failures
6. ✅ Provide fallback mechanisms
7. ✅ Localize all user-facing messages
8. ✅ Include error reference IDs for support
9. ✅ Test error scenarios thoroughly
10. ✅ Monitor and alert on error patterns

### Error Handling Don'ts:
1. ❌ Don't expose stack traces to users
2. ❌ Don't log sensitive data (passwords, cards)
3. ❌ Don't swallow exceptions silently
4. ❌ Don't use exceptions for control flow
5. ❌ Don't retry non-idempotent operations blindly
6. ❌ Don't show technical jargon to end users
7. ❌ Don't ignore validation errors
8. ❌ Don't let plugins crash the application
9. ❌ Don't make user wait during long retries
10. ❌ Don't forget to test error paths

---

**Related Documentation**:
- [Business Logic](business-logic.md) - Business rule enforcement
- [Workflows](workflows.md) - Error handling in workflows
- [Decision Logic](decision-logic.md) - Validation decision trees

---

**Document Version**: 1.0  
**Analysis Method**: Static code analysis of error handling patterns
