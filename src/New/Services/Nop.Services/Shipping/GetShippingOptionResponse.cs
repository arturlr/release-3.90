using Nop.Core.Domain.Shipping;

namespace Nop.Services.Shipping;

/// <summary>
/// Represents a response of getting shipping rate options.
/// </summary>
public class GetShippingOptionResponse
{
    public IList<ShippingOption> ShippingOptions { get; set; } = [];
    public bool ShippingFromMultipleLocations { get; set; }
    public IList<string> Errors { get; set; } = [];
    public bool Success => Errors.Count == 0;

    public void AddError(string error) => Errors.Add(error);
}
