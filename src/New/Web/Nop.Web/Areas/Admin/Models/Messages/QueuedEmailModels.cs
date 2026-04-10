using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Messages;

public class QueuedEmailListModel : BaseNopModel
{
    public DateTime? SearchStartDate { get; set; }
    public DateTime? SearchEndDate { get; set; }
    public string? SearchFromEmail { get; set; }
    public string? SearchToEmail { get; set; }
    public bool SearchLoadNotSent { get; set; }
    public int SearchMaxSentTries { get; set; } = 10;
    public int GoDirectlyToNumber { get; set; }
}

public class QueuedEmailModel : BaseNopEntityModel
{
    public string? PriorityName { get; set; }
    public string? From { get; set; }
    public string? FromName { get; set; }
    public string? To { get; set; }
    public string? ToName { get; set; }
    public string? ReplyTo { get; set; }
    public string? ReplyToName { get; set; }
    public string? CC { get; set; }
    public string? Bcc { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? AttachmentFilePath { get; set; }
    public string? AttachmentFileName { get; set; }
    public int AttachedDownloadId { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool SendImmediately { get; set; }
    public DateTime? DontSendBeforeDate { get; set; }
    public int SentTries { get; set; }
    public DateTime? SentOn { get; set; }
    public string? EmailAccountName { get; set; }
    public int EmailAccountId { get; set; }
}

public class QueuedEmailGridModel
{
    public int Id { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Subject { get; set; }
    public DateTime CreatedOn { get; set; }
    public int SentTries { get; set; }
    public DateTime? SentOn { get; set; }
    public string? PriorityName { get; set; }
}
