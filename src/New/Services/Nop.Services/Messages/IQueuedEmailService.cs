using Nop.Core;
using Nop.Core.Domain.Messages;

namespace Nop.Services.Messages;

public interface IQueuedEmailService
{
    Task<QueuedEmail?> GetQueuedEmailByIdAsync(int queuedEmailId);
    Task<IList<QueuedEmail>> GetQueuedEmailsByIdsAsync(int[] queuedEmailIds);
    Task<IPagedList<QueuedEmail>> SearchEmailsAsync(
        string? fromEmail = null, string? toEmail = null,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        bool loadNotSentItemsOnly = false, bool loadOnlyItemsToBeSent = false,
        int maxSendTries = int.MaxValue, bool loadNewest = false,
        int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertQueuedEmailAsync(QueuedEmail queuedEmail);
    Task UpdateQueuedEmailAsync(QueuedEmail queuedEmail);
    Task DeleteQueuedEmailAsync(QueuedEmail queuedEmail);
    Task DeleteQueuedEmailsAsync(IList<QueuedEmail> queuedEmails);
    Task DeleteAllEmailsAsync();
}
