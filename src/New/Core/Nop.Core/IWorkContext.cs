using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;

namespace Nop.Core;

/// <summary>
/// Work context — provides current request's customer, language, currency, tax display type
/// </summary>
public interface IWorkContext
{
    Customer CurrentCustomer { get; set; }
    Customer? OriginalCustomerIfImpersonated { get; }
    Vendor? CurrentVendor { get; }
    Language WorkingLanguage { get; set; }
    Currency WorkingCurrency { get; set; }
    TaxDisplayType TaxDisplayType { get; set; }
    bool IsAdmin { get; set; }
}
