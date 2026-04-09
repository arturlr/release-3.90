using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Messages;
using Nop.Services.Events;

namespace Nop.Services.Messages;

public class QueuedEmailService(
    IRepository<QueuedEmail> queuedEmailRepository,
    IEventPublisher eventPublisher) : IQueuedEmailService
{
    public Task<QueuedEmail?> GetQueuedEmailByIdAsync(int queuedEmailId)
    {
        if (queuedEmailId == 0)
            return Task.FromResult<QueuedEmail?>(null);

        return Task.FromResult<QueuedEmail?>(queuedEmailRepository.GetById(queuedEmailId));
    }

    public Task<IList<QueuedEmail>> GetQueuedEmailsByIdsAsync(int[] queuedEmailIds)
    {
        if (queuedEmailIds == null || queuedEmailIds.Length == 0)
            return Task.FromResult<IList<QueuedEmail>>([]);

        var queuedEmails = queuedEmailRepository.Table
            .Where(qe => queuedEmailIds.Contains(qe.Id))
            .ToList();

        // sort by passed identifiers
        var sorted = queuedEmailIds
            .Select(id => queuedEmails.Find(x => x.Id == id))
            .Where(qe => qe != null)
            .Cast<QueuedEmail>()
            .ToList();

        return Task.FromResult<IList<QueuedEmail>>(sorted);
    }

    public Task<IPagedList<QueuedEmail>> SearchEmailsAsync(
        string? fromEmail = null, string? toEmail = null,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        bool loadNotSentItemsOnly = false, bool loadOnlyItemsToBeSent = false,
        int maxSendTries = int.MaxValue, bool loadNewest = false,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        fromEmail = (fromEmail ?? string.Empty).Trim();
        toEmail = (toEmail ?? string.Empty).Trim();

        var query = queuedEmailRepository.Table;

        if (!string.IsNullOrEmpty(fromEmail))
            query = query.Where(qe => qe.From!.Contains(fromEmail));
        if (!string.IsNullOrEmpty(toEmail))
            query = query.Where(qe => qe.To!.Contains(toEmail));
        if (createdFromUtc.HasValue)
            query = query.Where(qe => qe.CreatedOnUtc >= createdFromUtc.Value);
        if (createdToUtc.HasValue)
            query = query.Where(qe => qe.CreatedOnUtc <= createdToUtc.Value);
        if (loadNotSentItemsOnly)
            query = query.Where(qe => !qe.SentOnUtc.HasValue);
        if (loadOnlyItemsToBeSent)
        {
            var nowUtc = DateTime.UtcNow;
            query = query.Where(qe => !qe.DontSendBeforeDateUtc.HasValue || qe.DontSendBeforeDateUtc.Value <= nowUtc);
        }
        query = query.Where(qe => qe.SentTries < maxSendTries);

        query = loadNewest
            ? query.OrderByDescending(qe => qe.CreatedOnUtc)
            : query.OrderByDescending(qe => qe.PriorityId).ThenBy(qe => qe.CreatedOnUtc);

        return Task.FromResult<IPagedList<QueuedEmail>>(new PagedList<QueuedEmail>(query, pageIndex, pageSize));
    }

    public async Task InsertQueuedEmailAsync(QueuedEmail queuedEmail)
    {
        ArgumentNullException.ThrowIfNull(queuedEmail);
        queuedEmailRepository.Insert(queuedEmail);
        await eventPublisher.EntityInsertedAsync(queuedEmail);
    }

    public async Task UpdateQueuedEmailAsync(QueuedEmail queuedEmail)
    {
        ArgumentNullException.ThrowIfNull(queuedEmail);
        queuedEmailRepository.Update(queuedEmail);
        await eventPublisher.EntityUpdatedAsync(queuedEmail);
    }

    public async Task DeleteQueuedEmailAsync(QueuedEmail queuedEmail)
    {
        ArgumentNullException.ThrowIfNull(queuedEmail);
        queuedEmailRepository.Delete(queuedEmail);
        await eventPublisher.EntityDeletedAsync(queuedEmail);
    }

    public async Task DeleteQueuedEmailsAsync(IList<QueuedEmail> queuedEmails)
    {
        ArgumentNullException.ThrowIfNull(queuedEmails);
        queuedEmailRepository.Delete(queuedEmails);
        foreach (var qe in queuedEmails)
            await eventPublisher.EntityDeletedAsync(qe);
    }

    public Task DeleteAllEmailsAsync()
    {
        var all = queuedEmailRepository.Table.ToList();
        queuedEmailRepository.Delete(all);
        return Task.CompletedTask;
    }
}
