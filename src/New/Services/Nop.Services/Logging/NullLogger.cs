using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;

namespace Nop.Services.Logging;

/// <summary>
/// Null logger — no-op implementation for use when DB logging is disabled.
/// </summary>
public class NullLogger : INopLogger
{
    public bool IsEnabled(LogLevel level) => false;
    public void DeleteLog(Log log) { }
    public void DeleteLogs(IList<Log> logs) { }
    public void ClearLog() { }

    public IPagedList<Log> GetAllLogs(DateTime? fromUtc = null, DateTime? toUtc = null,
        string message = "", LogLevel? logLevel = null,
        int pageIndex = 0, int pageSize = int.MaxValue) =>
        new PagedList<Log>(new List<Log>(), pageIndex, pageSize);

    public Log? GetLogById(int logId) => null;
    public IList<Log> GetLogByIds(int[] logIds) => [];
    public Log? InsertLog(LogLevel logLevel, string shortMessage, string fullMessage = "", Customer? customer = null) => null;
}
