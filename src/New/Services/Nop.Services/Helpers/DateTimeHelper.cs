using System.Collections.ObjectModel;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Helpers;

public class DateTimeHelper : IDateTimeHelper
{
    private readonly IWorkContext _workContext;
    private readonly IRepository<GenericAttribute> _genericAttributeRepository;
    private readonly DateTimeSettings _dateTimeSettings;

    public DateTimeHelper(
        IWorkContext workContext,
        IRepository<GenericAttribute> genericAttributeRepository,
        DateTimeSettings dateTimeSettings)
    {
        _workContext = workContext;
        _genericAttributeRepository = genericAttributeRepository;
        _dateTimeSettings = dateTimeSettings;
    }

    public virtual TimeZoneInfo FindTimeZoneById(string id) =>
        TimeZoneInfo.FindSystemTimeZoneById(id);

    public virtual ReadOnlyCollection<TimeZoneInfo> GetSystemTimeZones() =>
        TimeZoneInfo.GetSystemTimeZones();

    public virtual DateTime ConvertToUserTime(DateTime dt) =>
        ConvertToUserTime(dt, dt.Kind);

    public virtual DateTime ConvertToUserTime(DateTime dt, DateTimeKind sourceDateTimeKind)
    {
        dt = DateTime.SpecifyKind(dt, sourceDateTimeKind);
        return TimeZoneInfo.ConvertTime(dt, CurrentTimeZone);
    }

    public virtual DateTime ConvertToUserTime(DateTime dt, TimeZoneInfo sourceTimeZone) =>
        ConvertToUserTime(dt, sourceTimeZone, CurrentTimeZone);

    public virtual DateTime ConvertToUserTime(DateTime dt, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone) =>
        TimeZoneInfo.ConvertTime(dt, sourceTimeZone, destinationTimeZone);

    public virtual DateTime ConvertToUtcTime(DateTime dt) =>
        ConvertToUtcTime(dt, dt.Kind);

    public virtual DateTime ConvertToUtcTime(DateTime dt, DateTimeKind sourceDateTimeKind)
    {
        dt = DateTime.SpecifyKind(dt, sourceDateTimeKind);
        return TimeZoneInfo.ConvertTimeToUtc(dt);
    }

    public virtual DateTime ConvertToUtcTime(DateTime dt, TimeZoneInfo sourceTimeZone) =>
        sourceTimeZone.IsInvalidTime(dt) ? dt : TimeZoneInfo.ConvertTimeToUtc(dt, sourceTimeZone);

    public virtual TimeZoneInfo GetCustomerTimeZone(Customer customer)
    {
        if (_dateTimeSettings.AllowCustomersToSetTimeZone && customer != null)
        {
            var timeZoneId = _genericAttributeRepository.TableNoTracking
                .Where(ga => ga.EntityId == customer.Id
                    && ga.KeyGroup == nameof(Customer)
                    && ga.Key == SystemCustomerAttributeNames.TimeZoneId
                    && ga.StoreId == 0)
                .Select(ga => ga.Value)
                .FirstOrDefault();

            if (!string.IsNullOrEmpty(timeZoneId))
            {
                try { return FindTimeZoneById(timeZoneId); }
                catch { /* invalid timezone id — fall through to default */ }
            }
        }

        return DefaultStoreTimeZone;
    }

    public virtual TimeZoneInfo DefaultStoreTimeZone
    {
        get
        {
            if (!string.IsNullOrEmpty(_dateTimeSettings.DefaultStoreTimeZoneId))
            {
                try { return FindTimeZoneById(_dateTimeSettings.DefaultStoreTimeZoneId); }
                catch { /* invalid timezone id — fall through to UTC */ }
            }
            return TimeZoneInfo.Utc;
        }
    }

    public virtual TimeZoneInfo CurrentTimeZone =>
        GetCustomerTimeZone(_workContext.CurrentCustomer);
}
