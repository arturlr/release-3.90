namespace Nop.Services.Shipping;

/// <summary>
/// Shipment status event (used by shipment trackers).
/// </summary>
public class ShipmentStatusEvent
{
    public string? EventName { get; set; }
    public string? Location { get; set; }
    public string? CountryCode { get; set; }
    public DateTime? Date { get; set; }
}
