using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Logging;

namespace Nop.Services.Logging;

/// <summary>
/// Default logger — stores application logs in the database.
/// </summary>
public class DefaultLogger : INopLogger
{
    private readonly IRepository<Log> _logRepository;
    private readonly IWebHelper _webHelper;

    public DefaultLogger(IRepository<Log> logRepository, IWebHelper webHelper)
    {
        _logRepository = logRepository;
        _webHelper = webHelper;
    }

    public virtual bool IsEnabled(LogLevel level) => level != LogLevel.Debug;

    public virtual void DeleteLog(Log log)
    {
        ArgumentNullException.ThrowIfNull(log);
        _logRepository.Delete(log);
    }

    public virtual void DeleteLogs(IList<Log> logs)
    {
        ArgumentNullException.ThrowIfNull(logs);
        _logRepository.Delete(logs);
    }

    public virtual void ClearLog()
    {
        var all = _logRepository.Table.ToList();
        _logRepository.Delete(all);
    }

    public virtual IPagedList<Log> GetAllLogs(DateTime? fromUtc = null, DateTime? toUtc = null,
        string message = "", LogLevel? logLevel = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _logRepository.Table;

        if (fromUtc.HasValue)
            query = query.Where(l => fromUtc.Value <= l.CreatedOnUtc);
        if (toUtc.HasValue)
            query = query.Where(l => toUtc.Value >= l.CreatedOnUtc);
        if (logLevel.HasValue)
        {
            var logLevelId = (int)logLevel.Value;
            query = query.Where(l => logLevelId == l.LogLevelId);
        }
        if (!string.IsNullOrEmpty(message))
            query = query.Where(l => l.ShortMessage!.Contains(message) || l.FullMessage!.Contains(message));

        query = query.OrderByDescending(l => l.CreatedOnUtc);
        return new PagedList<Log>(query, pageIndex, pageSize);
    }

    public virtual Log? GetLogById(int logId) =>
        logId == 0 ? null : _logRepository.GetById(logId);

    public virtual IList<Log> GetLogByIds(int[] logIds)
    {
        if (logIds is null || logIds.Length == 0)
            return [];

        var query = from l in _logRepository.Table
                    where logIds.Contains(l.Id)
                    select l;

        var logItems = query.ToList();
        // preserve requested order
        return logIds
            .Select(id => logItems.Find(x => x.Id == id))
            .Where(l => l is not null)
            .ToList()!;
    }

    public virtual Log? InsertLog(LogLevel logLevel, string shortMessage,
        string fullMessage = "", Customer? customer = null)
    {
        var log = new Log
        {
            LogLevel = logLevel,
            ShortMessage = shortMessage,
            FullMessage = fullMessage,
            IpAddress = _webHelper.GetCurrentIpAddress(),
            CustomerId = customer?.Id,
            PageUrl = _webHelper.GetThisPageUrl(true),
            ReferrerUrl = _webHelper.GetUrlReferrer(),
            CreatedOnUtc = DateTime.UtcNow
        };

        _logRepository.Insert(log);
        return log;
    }
}
