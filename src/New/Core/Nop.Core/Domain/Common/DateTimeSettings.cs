using Nop.Core.Configuration;

namespace Nop.Core.Domain.Common;

public class DateTimeSettings : ISettings
{
    /// <summary>
    /// Gets or sets a default store time zone identifier
    /// </summary>
    public string? DefaultStoreTimeZoneId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether customers are allowed to select their time zone
    /// </summary>
    public bool AllowCustomersToSetTimeZone { get; set; }
}
