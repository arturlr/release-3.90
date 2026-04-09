using System.Collections.ObjectModel;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Helpers;

/// <summary>
/// Date/time helper — UTC ↔ user timezone conversion
/// </summary>
public interface IDateTimeHelper
{
    TimeZoneInfo FindTimeZoneById(string id);
    ReadOnlyCollection<TimeZoneInfo> GetSystemTimeZones();

    DateTime ConvertToUserTime(DateTime dt);
    DateTime ConvertToUserTime(DateTime dt, DateTimeKind sourceDateTimeKind);
    DateTime ConvertToUserTime(DateTime dt, TimeZoneInfo sourceTimeZone);
    DateTime ConvertToUserTime(DateTime dt, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone);

    DateTime ConvertToUtcTime(DateTime dt);
    DateTime ConvertToUtcTime(DateTime dt, DateTimeKind sourceDateTimeKind);
    DateTime ConvertToUtcTime(DateTime dt, TimeZoneInfo sourceTimeZone);

    TimeZoneInfo GetCustomerTimeZone(Customer customer);

    TimeZoneInfo DefaultStoreTimeZone { get; }
    TimeZoneInfo CurrentTimeZone { get; }
}
