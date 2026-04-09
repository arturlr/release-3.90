using Nop.Core.Domain.Shipping;

namespace Nop.Services.Shipping;

/// <summary>
/// Represents a response of getting pickup points.
/// </summary>
public class GetPickupPointsResponse
{
    public IList<PickupPoint> PickupPoints { get; set; } = [];
    public IList<string> Errors { get; set; } = [];
    public bool Success => Errors.Count == 0;

    public void AddError(string error) => Errors.Add(error);
}
