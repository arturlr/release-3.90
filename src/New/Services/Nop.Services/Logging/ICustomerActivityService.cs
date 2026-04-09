using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;

namespace Nop.Services.Logging;

/// <summary>
/// Customer activity service — tracks admin and customer actions for audit logging.
/// </summary>
public interface ICustomerActivityService
{
    void InsertActivityType(ActivityLogType activityLogType);
    void UpdateActivityType(ActivityLogType activityLogType);
    void DeleteActivityType(ActivityLogType activityLogType);
    IList<ActivityLogType> GetAllActivityTypes();
    ActivityLogType? GetActivityTypeById(int activityLogTypeId);
    ActivityLog? InsertActivity(string systemKeyword, string comment, params object[] commentParams);
    ActivityLog? InsertActivity(Customer customer, string systemKeyword, string comment, params object[] commentParams);
    void DeleteActivity(ActivityLog activityLog);
    IPagedList<ActivityLog> GetAllActivities(DateTime? createdOnFrom = null,
        DateTime? createdOnTo = null, int? customerId = null, int activityLogTypeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue, string? ipAddress = null);
    ActivityLog? GetActivityById(int activityLogId);
    void ClearAllActivities();
}
