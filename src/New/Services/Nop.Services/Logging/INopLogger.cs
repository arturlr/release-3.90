using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;

namespace Nop.Services.Logging;

/// <summary>
/// Nop application logger — stores logs in database for admin UI.
/// Renamed from legacy ILogger to avoid conflict with Microsoft.Extensions.Logging.ILogger.
/// </summary>
public interface INopLogger
{
    bool IsEnabled(LogLevel level);
    void DeleteLog(Log log);
    void DeleteLogs(IList<Log> logs);
    void ClearLog();
    IPagedList<Log> GetAllLogs(DateTime? fromUtc = null, DateTime? toUtc = null,
        string message = "", LogLevel? logLevel = null,
        int pageIndex = 0, int pageSize = int.MaxValue);
    Log? GetLogById(int logId);
    IList<Log> GetLogByIds(int[] logIds);
    Log? InsertLog(LogLevel logLevel, string shortMessage, string fullMessage = "", Customer? customer = null);
}
