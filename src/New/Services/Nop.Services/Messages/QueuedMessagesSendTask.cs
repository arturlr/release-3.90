using Microsoft.Extensions.Logging;
using Nop.Services.Tasks;

namespace Nop.Services.Messages;

public class QueuedMessagesSendTask(
    IQueuedEmailService queuedEmailService,
    IEmailSender emailSender,
    IEmailAccountService emailAccountService,
    ILogger<QueuedMessagesSendTask> logger) : ITask
{
    public async Task ExecuteAsync()
    {
        const int maxTries = 3;
        var queuedEmails = await queuedEmailService.SearchEmailsAsync(
            loadNotSentItemsOnly: true,
            loadOnlyItemsToBeSent: true,
            maxSendTries: maxTries,
            loadNewest: false,
            pageSize: 500);

        foreach (var qe in queuedEmails)
        {
            var bcc = string.IsNullOrWhiteSpace(qe.Bcc) ? null
                : qe.Bcc.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var cc = string.IsNullOrWhiteSpace(qe.CC) ? null
                : qe.CC.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            try
            {
                var emailAccount = await emailAccountService.GetEmailAccountByIdAsync(qe.EmailAccountId);
                if (emailAccount == null) continue;

                await emailSender.SendEmailAsync(emailAccount,
                    qe.Subject ?? string.Empty, qe.Body ?? string.Empty,
                    qe.From ?? string.Empty, qe.FromName ?? string.Empty,
                    qe.To ?? string.Empty, qe.ToName ?? string.Empty,
                    qe.ReplyTo, qe.ReplyToName,
                    bcc, cc,
                    qe.AttachmentFilePath, qe.AttachmentFileName,
                    qe.AttachedDownloadId);

                qe.SentOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending e-mail. {Message}", ex.Message);
            }
            finally
            {
                qe.SentTries++;
                await queuedEmailService.UpdateQueuedEmailAsync(qe);
            }
        }
    }
}
