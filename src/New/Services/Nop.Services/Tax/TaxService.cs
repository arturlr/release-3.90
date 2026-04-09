using System.Text.RegularExpressions;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Logging;

namespace Nop.Services.Tax;

public partial class TaxService : ITaxService
{
    private readonly IAddressService _addressService;
    private readonly IWorkContext _workContext;
    private readonly IStoreContext _storeContext;
    private readonly TaxSettings _taxSettings;
    private readonly IGeoLookupService _geoLookupService;
    private readonly ICountryService _countryService;
    private readonly IStateProvinceService _stateProvinceService;
    private readonly INopLogger _logger;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IRepository<CustomerRole> _customerRoleRepository;
    private readonly CustomerSettings _customerSettings;
    private readonly ShippingSettings _shippingSettings;
    private readonly AddressSettings _addressSettings;

    public TaxService(
        IAddressService addressService,
        IWorkContext workContext,
        IStoreContext storeContext,
        TaxSettings taxSettings,
        IGeoLookupService geoLookupService,
        ICountryService countryService,
        IStateProvinceService stateProvinceService,
        INopLogger logger,
        IGenericAttributeService genericAttributeService,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IRepository<CustomerRole> customerRoleRepository,
        CustomerSettings customerSettings,
        ShippingSettings shippingSettings,
        AddressSettings addressSettings)
    {
        _addressService = addressService;
        _workContext = workContext;
        _storeContext = storeContext;
        _taxSettings = taxSettings;
        _geoLookupService = geoLookupService;
        _countryService = countryService;
        _stateProvinceService = stateProvinceService;
        _logger = logger;
        _genericAttributeService = genericAttributeService;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _customerRoleRepository = customerRoleRepository;
        _customerSettings = customerSettings;
        _shippingSettings = shippingSettings;
        _addressSettings = addressSettings;
    }

    #region Utilities

    /// <summary>
    /// Check if customer is an EU consumer (person, not a validated business) for EU VAT digital services rule.
    /// </summary>
    protected virtual async Task<bool> IsEuConsumerAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        Country? country = null;

        // Try billing address country
        if (_addressSettings.CountryEnabled && customer.BillingAddressId is > 0)
        {
            var billingAddress = await _addressService.GetAddressByIdAsync(customer.BillingAddressId.Value);
            if (billingAddress?.CountryId is > 0)
                country = await _countryService.GetCountryByIdAsync(billingAddress.CountryId.Value);
        }

        // Try registration country
        if (country == null && _customerSettings.CountryEnabled)
        {
            var countryId = await customer.GetAttributeAsync<int>(
                SystemCustomerAttributeNames.CountryId, _genericAttributeService);
            if (countryId > 0)
                country = await _countryService.GetCountryByIdAsync(countryId);
        }

        // Try IP-based geolocation
        if (country == null)
        {
            var isoCode = _geoLookupService.LookupCountryIsoCode(customer.LastIpAddress ?? string.Empty);
            if (!string.IsNullOrEmpty(isoCode))
                country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(isoCode);
        }

        if (country == null || !country.SubjectToVat)
            return false;

        // If customer has a valid VAT number, they're a business — not a consumer
        var vatStatus = (VatNumberStatus)(await customer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.VatNumberStatusId, _genericAttributeService));
        return vatStatus != VatNumberStatus.Valid;
    }

    /// <summary>
    /// Build a CalculateTaxRequest with the correct address based on TaxSettings.TaxBasedOn.
    /// </summary>
    protected virtual async Task<CalculateTaxRequest> CreateCalculateTaxRequestAsync(
        Product? product, int taxCategoryId, Customer customer, decimal price)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var request = new CalculateTaxRequest
        {
            Customer = customer,
            Product = product,
            Price = price,
            TaxCategoryId = taxCategoryId > 0 ? taxCategoryId : (product?.TaxCategoryId ?? 0)
        };

        var basedOn = _taxSettings.TaxBasedOn;

        // EU VAT digital services rule (post-2015): charge VAT where the consumer is located
        var overridden = _taxSettings.EuVatEnabled
            && product is { IsTelecommunicationsOrBroadcastingOrElectronicServices: true }
            && await IsEuConsumerAsync(customer);
        if (overridden)
            basedOn = TaxBasedOn.BillingAddress;

        // Tax based on pickup point address
        if (!overridden && _taxSettings.TaxBasedOnPickupPointAddress && _shippingSettings.AllowPickUpInStore)
        {
            var pickupPoint = await customer.GetAttributeAsync<PickupPoint>(
                SystemCustomerAttributeNames.SelectedPickupPoint, _genericAttributeService,
                _storeContext.CurrentStore.Id);
            if (pickupPoint != null)
            {
                var country = await _countryService.GetCountryByTwoLetterIsoCodeAsync(pickupPoint.CountryCode ?? string.Empty);
                var state = await _stateProvinceService.GetStateProvinceByAbbreviationAsync(pickupPoint.StateAbbreviation ?? string.Empty);

                request.Address = new Address
                {
                    Address1 = pickupPoint.Address,
                    City = pickupPoint.City,
                    CountryId = country?.Id,
                    StateProvinceId = state?.Id,
                    ZipPostalCode = pickupPoint.ZipPostalCode,
                    CreatedOnUtc = DateTime.UtcNow
                };
                return request;
            }
        }

        // Fall back to default address if billing/shipping not set
        if (basedOn == TaxBasedOn.BillingAddress && customer.BillingAddressId is null or 0)
            basedOn = TaxBasedOn.DefaultAddress;
        if (basedOn == TaxBasedOn.ShippingAddress && customer.ShippingAddressId is null or 0)
            basedOn = TaxBasedOn.DefaultAddress;

        request.Address = basedOn switch
        {
            TaxBasedOn.BillingAddress => await _addressService.GetAddressByIdAsync(customer.BillingAddressId!.Value),
            TaxBasedOn.ShippingAddress => await _addressService.GetAddressByIdAsync(customer.ShippingAddressId!.Value),
            _ => await _addressService.GetAddressByIdAsync(_taxSettings.DefaultTaxAddressId)
        };

        return request;
    }

    /// <summary>
    /// Apply or remove tax from a price.
    /// </summary>
    protected static decimal CalculatePrice(decimal price, decimal percent, bool increase)
    {
        if (percent == decimal.Zero)
            return price;

        return increase
            ? price * (1 + percent / 100)
            : price - price / (100 + percent) * percent;
    }

    /// <summary>
    /// Get the tax rate for a product/customer combination.
    /// </summary>
    protected virtual async Task<(decimal taxRate, bool isTaxable)> GetTaxRateAsync(
        Product? product, int taxCategoryId, Customer customer, decimal price)
    {
        // No active tax provider — tax provider loading deferred to [2.10] plugin system.
        // For now, use the first registered ITaxProvider from DI (if any).
        // Until a provider is registered, taxRate = 0.
        var request = await CreateCalculateTaxRequestAsync(product, taxCategoryId, customer, price);

        if (await IsTaxExemptAsync(product, request.Customer))
            return (decimal.Zero, false);

        if (_taxSettings.EuVatEnabled && await IsVatExemptAsync(request.Address, request.Customer))
            return (decimal.Zero, false);

        // Tax provider call — deferred to [2.10] plugin system.
        // When no provider is available, return 0% tax rate (non-taxable behavior).
        // TODO: integrate ITaxProvider resolution via plugin system
        return (decimal.Zero, true);
    }

    #endregion

    #region Product price

    public virtual async Task<(decimal price, decimal taxRate)> GetProductPriceAsync(
        Product product, decimal price)
    {
        var customer = _workContext.CurrentCustomer;
        return await GetProductPriceAsync(product, price, customer);
    }

    public virtual async Task<(decimal price, decimal taxRate)> GetProductPriceAsync(
        Product product, decimal price, Customer customer)
    {
        bool includingTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax;
        return await GetProductPriceAsync(product, price, includingTax, customer, _taxSettings.PricesIncludeTax);
    }

    public virtual Task<(decimal price, decimal taxRate)> GetProductPriceAsync(
        Product product, decimal price, bool includingTax, Customer customer, bool priceIncludesTax)
    {
        return GetProductPriceAsync(product, 0, price, includingTax, customer, priceIncludesTax);
    }

    public virtual async Task<(decimal price, decimal taxRate)> GetProductPriceAsync(
        Product? product, int taxCategoryId, decimal price,
        bool includingTax, Customer customer, bool priceIncludesTax)
    {
        if (price == decimal.Zero)
            return (decimal.Zero, decimal.Zero);

        var (taxRate, isTaxable) = await GetTaxRateAsync(product, taxCategoryId, customer, price);

        if (priceIncludesTax)
        {
            if (includingTax)
            {
                if (!isTaxable)
                    price = CalculatePrice(price, taxRate, false);
            }
            else
            {
                price = CalculatePrice(price, taxRate, false);
            }
        }
        else
        {
            if (includingTax && isTaxable)
                price = CalculatePrice(price, taxRate, true);
        }

        if (!isTaxable)
            taxRate = decimal.Zero;

        return (price, taxRate);
    }

    #endregion

    #region Shipping price

    public virtual async Task<(decimal price, decimal taxRate)> GetShippingPriceAsync(
        decimal price, bool includingTax, Customer customer)
    {
        if (!_taxSettings.ShippingIsTaxable)
            return (price, decimal.Zero);

        return await GetProductPriceAsync(null, _taxSettings.ShippingTaxClassId,
            price, includingTax, customer, _taxSettings.ShippingPriceIncludesTax);
    }

    #endregion

    #region Payment additional fee

    public virtual async Task<(decimal price, decimal taxRate)> GetPaymentMethodAdditionalFeeAsync(
        decimal price, bool includingTax, Customer customer)
    {
        if (!_taxSettings.PaymentMethodAdditionalFeeIsTaxable)
            return (price, decimal.Zero);

        return await GetProductPriceAsync(null, _taxSettings.PaymentMethodAdditionalFeeTaxClassId,
            price, includingTax, customer, _taxSettings.PaymentMethodAdditionalFeeIncludesTax);
    }

    #endregion

    #region Checkout attribute price

    public virtual async Task<(decimal price, decimal taxRate)> GetCheckoutAttributePriceAsync(
        CheckoutAttributeValue cav, CheckoutAttribute checkoutAttribute,
        bool includingTax, Customer customer)
    {
        ArgumentNullException.ThrowIfNull(cav);
        ArgumentNullException.ThrowIfNull(checkoutAttribute);

        decimal price = cav.PriceAdjustment;
        if (checkoutAttribute.IsTaxExempt)
            return (price, decimal.Zero);

        return await GetProductPriceAsync(null, checkoutAttribute.TaxCategoryId,
            price, includingTax, customer, _taxSettings.PricesIncludeTax);
    }

    #endregion

    #region VAT

    public virtual async Task<(VatNumberStatus status, string name, string address)> GetVatNumberStatusAsync(
        string fullVatNumber)
    {
        if (string.IsNullOrWhiteSpace(fullVatNumber))
            return (VatNumberStatus.Empty, string.Empty, string.Empty);

        fullVatNumber = fullVatNumber.Trim();

        // Parse "GB 111 1111 111" or "GB1111111111"
        var match = VatNumberRegex().Match(fullVatNumber);
        if (!match.Success)
            return (VatNumberStatus.Invalid, string.Empty, string.Empty);

        var twoLetterIsoCode = match.Groups[1].Value;
        var vatNumber = match.Groups[2].Value;

        return await GetVatNumberStatusAsync(twoLetterIsoCode, vatNumber);
    }

    public virtual async Task<(VatNumberStatus status, string name, string address)> GetVatNumberStatusAsync(
        string twoLetterIsoCode, string vatNumber)
    {
        if (string.IsNullOrEmpty(twoLetterIsoCode) || string.IsNullOrEmpty(vatNumber))
            return (VatNumberStatus.Empty, string.Empty, string.Empty);

        if (_taxSettings.EuVatAssumeValid)
            return (VatNumberStatus.Valid, string.Empty, string.Empty);

        if (!_taxSettings.EuVatUseWebService)
            return (VatNumberStatus.Unknown, string.Empty, string.Empty);

        var (status, name, address, _) = await DoVatCheckAsync(twoLetterIsoCode, vatNumber);
        return (status, name, address);
    }

    public virtual Task<(VatNumberStatus status, string name, string address, Exception? exception)> DoVatCheckAsync(
        string twoLetterIsoCode, string vatNumber)
    {
        // VIES SOAP integration deferred to [7.12].
        // Return Unknown until the HTTP client for VIES is implemented.
        return Task.FromResult<(VatNumberStatus, string, string, Exception?)>(
            (VatNumberStatus.Unknown, string.Empty, string.Empty, null));
    }

    [GeneratedRegex(@"^(\w{2})(.*)")]
    private static partial Regex VatNumberRegex();

    #endregion

    #region Exemptions

    public virtual Task<bool> IsTaxExemptAsync(Product? product, Customer? customer)
    {
        if (customer != null)
        {
            if (customer.IsTaxExempt)
                return Task.FromResult(true);

            // Check if any active customer role is tax exempt (no nav properties — use join)
            var hasTaxExemptRole = (
                from crm in _customerRoleMappingRepository.TableNoTracking
                join cr in _customerRoleRepository.TableNoTracking on crm.CustomerRoleId equals cr.Id
                where crm.CustomerId == customer.Id && cr.Active && cr.TaxExempt
                select cr.Id
            ).Any();

            if (hasTaxExemptRole)
                return Task.FromResult(true);
        }

        if (product != null && product.IsTaxExempt)
            return Task.FromResult(true);

        return Task.FromResult(false);
    }

    public virtual async Task<bool> IsVatExemptAsync(Address? address, Customer? customer)
    {
        if (!_taxSettings.EuVatEnabled)
            return false;

        if (address == null || customer == null)
            return false;

        if (address.CountryId is null or 0)
            return false;

        var country = await _countryService.GetCountryByIdAsync(address.CountryId.Value);
        if (country == null)
            return false;

        if (!country.SubjectToVat)
            return true; // Outside VAT zone — not chargeable

        // Inside VAT zone — exempt only if valid VAT number, different country from shop, and exemption allowed
        var vatStatus = (VatNumberStatus)(await customer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.VatNumberStatusId, _genericAttributeService));

        return address.CountryId != _taxSettings.EuVatShopCountryId
            && vatStatus == VatNumberStatus.Valid
            && _taxSettings.EuVatAllowVatExemption;
    }

    #endregion
}
