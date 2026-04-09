using Nop.Core.Configuration;

namespace Nop.Core.Domain.Shipping;

public class ShippingSettings : ISettings
{
    public List<string> ActiveShippingRateComputationMethodSystemNames { get; set; } = [];
    public List<string> ActivePickupPointProviderSystemNames { get; set; } = [];
    public bool ShipToSameAddress { get; set; }
    public bool AllowPickUpInStore { get; set; }
    public bool DisplayPickupPointsOnMap { get; set; }
    public string? GoogleMapsApiKey { get; set; }
    public bool UseWarehouseLocation { get; set; }
    public bool NotifyCustomerAboutShippingFromMultipleLocations { get; set; }
    public bool FreeShippingOverXEnabled { get; set; }
    public decimal FreeShippingOverXValue { get; set; }
    public bool FreeShippingOverXIncludingTax { get; set; }
    public bool EstimateShippingEnabled { get; set; }
    public bool DisplayShipmentEventsToCustomers { get; set; }
    public bool DisplayShipmentEventsToStoreOwner { get; set; }
    public bool HideShippingTotal { get; set; }
    public int ShippingOriginAddressId { get; set; }
    public bool ReturnValidOptionsIfThereAreAny { get; set; }
    public bool BypassShippingMethodSelectionIfOnlyOne { get; set; }
    public bool UseCubeRootMethod { get; set; }
    public bool ConsiderAssociatedProductsDimensions { get; set; }
}
