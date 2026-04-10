using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Messages;

public class MessageTemplateListModel
{
    public int SearchStoreId { get; set; }
    public List<StoreSelectItem> AvailableStores { get; set; } = [];

    public class StoreSelectItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}

public class MessageTemplateModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? BccEmailAddresses { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public bool IsActive { get; set; }
    public bool SendImmediately { get; set; }
    public int? DelayBeforeSend { get; set; }
    public int DelayPeriodId { get; set; }
    public int AttachedDownloadId { get; set; }
    public bool HasAttachedDownload { get; set; }
    public int EmailAccountId { get; set; }
    public bool LimitedToStores { get; set; }
    public string? AllowedTokens { get; set; }
    public List<EmailAccountSelectItem> AvailableEmailAccounts { get; set; } = [];

    public class EmailAccountSelectItem
    {
        public int Id { get; set; }
        public string? DisplayName { get; set; }
    }
}

public class MessageTemplateGridModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsActive { get; set; }
    public string? ListOfStores { get; set; }
}

public class TestMessageTemplateModel
{
    public int Id { get; set; }
    public int LanguageId { get; set; }
    public string? SendTo { get; set; }
    public List<string> Tokens { get; set; } = [];
}
