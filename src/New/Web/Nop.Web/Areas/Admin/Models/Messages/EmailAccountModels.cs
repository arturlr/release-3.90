using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Messages;

public class EmailAccountModel : BaseNopEntityModel
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; }
    public bool UseDefaultCredentials { get; set; }
    public bool IsDefaultEmailAccount { get; set; }
    public string? SendTestEmailTo { get; set; }
}
