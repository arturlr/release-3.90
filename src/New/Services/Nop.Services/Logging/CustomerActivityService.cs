using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;

namespace Nop.Services.Logging;

/// <summary>
/// Customer activity service — tracks admin and customer actions for audit logging.
/// </summary>
public class CustomerActivityService : ICustomerActivityService
{
    private const string ActivityTypeAllKey = "Nop.activitytype.all";
    private const string ActivityTypePrefixKey = "Nop.activitytype.";

    private readonly IStaticCacheManager _cacheManager;
    private readonly IRepository<ActivityLog> _activityLogRepository;
    private readonly IRepository<ActivityLogType> _activityLogTypeRepository;
    private readonly IWorkContext _workContext;
    private readonly IWebHelper _webHelper;

    public CustomerActivityService(
        IStaticCacheManager cacheManager,
        IRepository<ActivityLog> activityLogRepository,
        IRepository<ActivityLogType> activityLogTypeRepository,
        IWorkContext workContext,
        IWebHelper webHelper)
    {
        _cacheManager = cacheManager;
        _activityLogRepository = activityLogRepository;
        _activityLogTypeRepository = activityLogTypeRepository;
        _workContext = workContext;
        _webHelper = webHelper;
    }

    public virtual void InsertActivityType(ActivityLogType activityLogType)
    {
        ArgumentNullException.ThrowIfNull(activityLogType);
        _activityLogTypeRepository.Insert(activityLogType);
        _cacheManager.RemoveByPrefixAsync(ActivityTypePrefixKey).GetAwaiter().GetResult();
    }

    public virtual void UpdateActivityType(ActivityLogType activityLogType)
    {
        ArgumentNullException.ThrowIfNull(activityLogType);
        _activityLogTypeRepository.Update(activityLogType);
        _cacheManager.RemoveByPrefixAsync(ActivityTypePrefixKey).GetAwaiter().GetResult();
    }

    public virtual void DeleteActivityType(ActivityLogType activityLogType)
    {
        ArgumentNullException.ThrowIfNull(activityLogType);
        _activityLogTypeRepository.Delete(activityLogType);
        _cacheManager.RemoveByPrefixAsync(ActivityTypePrefixKey).GetAwaiter().GetResult();
    }

    public virtual IList<ActivityLogType> GetAllActivityTypes()
    {
        return _activityLogTypeRepository.Table
            .OrderBy(alt => alt.Name)
            .ToList();
    }

    public virtual ActivityLogType? GetActivityTypeById(int activityLogTypeId) =>
        activityLogTypeId == 0 ? null : _activityLogTypeRepository.GetById(activityLogTypeId);

    public virtual ActivityLog? InsertActivity(string systemKeyword, string comment, params object[] commentParams) =>
        InsertActivity(_workContext.CurrentCustomer, systemKeyword, comment, commentParams);

    public virtual ActivityLog? InsertActivity(Customer customer, string systemKeyword, string comment, params object[] commentParams)
    {
        if (customer is null)
            return null;

        var activityTypes = _cacheManager.Get(
            new CacheKey(ActivityTypeAllKey, ActivityTypePrefixKey),
            () => GetAllActivityTypes());

        var activityType = activityTypes?.FirstOrDefault(at => at.SystemKeyword == systemKeyword);
        if (activityType is null || !activityType.Enabled)
            return null;

        comment = CommonHelper.EnsureNotNull(comment);
        comment = string.Format(comment, commentParams);
        comment = CommonHelper.EnsureMaximumLength(comment, 4000);

        var activity = new ActivityLog
        {
            ActivityLogTypeId = activityType.Id,
            CustomerId = customer.Id,
            Comment = comment,
            CreatedOnUtc = DateTime.UtcNow,
            IpAddress = _webHelper.GetCurrentIpAddress()
        };

        _activityLogRepository.Insert(activity);
        return activity;
    }

    public virtual void DeleteActivity(ActivityLog activityLog)
    {
        ArgumentNullException.ThrowIfNull(activityLog);
        _activityLogRepository.Delete(activityLog);
    }

    public virtual IPagedList<ActivityLog> GetAllActivities(DateTime? createdOnFrom = null,
        DateTime? createdOnTo = null, int? customerId = null, int activityLogTypeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue, string? ipAddress = null)
    {
        var query = _activityLogRepository.Table;

        if (!string.IsNullOrEmpty(ipAddress))
            query = query.Where(al => al.IpAddress!.Contains(ipAddress));
        if (createdOnFrom.HasValue)
            query = query.Where(al => createdOnFrom.Value <= al.CreatedOnUtc);
        if (createdOnTo.HasValue)
            query = query.Where(al => createdOnTo.Value >= al.CreatedOnUtc);
        if (activityLogTypeId > 0)
            query = query.Where(al => activityLogTypeId == al.ActivityLogTypeId);
        if (customerId.HasValue)
            query = query.Where(al => customerId.Value == al.CustomerId);

        query = query.OrderByDescending(al => al.CreatedOnUtc);
        return new PagedList<ActivityLog>(query, pageIndex, pageSize);
    }

    public virtual ActivityLog? GetActivityById(int activityLogId) =>
        activityLogId == 0 ? null : _activityLogRepository.GetById(activityLogId);

    public virtual void ClearAllActivities()
    {
        var all = _activityLogRepository.Table.ToList();
        _activityLogRepository.Delete(all);
    }
}
