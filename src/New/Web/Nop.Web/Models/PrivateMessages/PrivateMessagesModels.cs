using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.PrivateMessages;

public class PrivateMessageIndexModel
{
    public int InboxPage { get; set; }
    public int SentItemsPage { get; set; }
    public bool SentItemsTabSelected { get; set; }
    public PrivateMessageListModel InboxMessages { get; set; } = new();
    public PrivateMessageListModel SentMessages { get; set; } = new();
}

public class PrivateMessageListModel
{
    public IList<PrivateMessageModel> Messages { get; set; } = [];
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

public class PrivateMessageModel : BaseNopEntityModel
{
    public int FromCustomerId { get; set; }
    public string? CustomerFromName { get; set; }
    public int ToCustomerId { get; set; }
    public string? CustomerToName { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool IsRead { get; set; }
}

public class SendPrivateMessageModel : BaseNopEntityModel
{
    public int ToCustomerId { get; set; }
    public string? CustomerToName { get; set; }
    public int ReplyToMessageId { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
}
