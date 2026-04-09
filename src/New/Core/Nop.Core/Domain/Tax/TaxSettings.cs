using Nop.Core.Configuration;
using Nop.Core.Domain.Tax;

namespace Nop.Core.Domain.Tax;

public class TaxSettings : ISettings
{
    public TaxBasedOn TaxBasedOn { get; set; }
    public bool TaxBasedOnPickupPointAddress { get; set; }
    public TaxDisplayType TaxDisplayType { get; set; }
    public string? ActiveTaxProviderSystemName { get; set; }
    public int DefaultTaxAddressId { get; set; }
    public bool DisplayTaxSuffix { get; set; }
    public bool DisplayTaxRates { get; set; }
    public bool PricesIncludeTax { get; set; }
    public bool AllowCustomersToSelectTaxDisplayType { get; set; }
    public bool HideZeroTax { get; set; }
    public bool HideTaxInOrderSummary { get; set; }
    public bool ForceTaxExclusionFromOrderSubtotal { get; set; }
    public int DefaultTaxCategoryId { get; set; }
    public bool ShippingIsTaxable { get; set; }
    public bool ShippingPriceIncludesTax { get; set; }
    public int ShippingTaxClassId { get; set; }
    public bool PaymentMethodAdditionalFeeIsTaxable { get; set; }
    public bool PaymentMethodAdditionalFeeIncludesTax { get; set; }
    public int PaymentMethodAdditionalFeeTaxClassId { get; set; }
    public bool EuVatEnabled { get; set; }
    public int EuVatShopCountryId { get; set; }
    public bool EuVatAllowVatExemption { get; set; }
    public bool EuVatUseWebService { get; set; }
    public bool EuVatAssumeValid { get; set; }
    public bool EuVatEmailAdminWhenNewVatSubmitted { get; set; }
    public bool LogErrors { get; set; }
}
