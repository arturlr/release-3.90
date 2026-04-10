using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Customer;

public class PasswordRecoveryModel : BaseNopModel
{
    public string? Email { get; set; }
    public string? Result { get; set; }
}
