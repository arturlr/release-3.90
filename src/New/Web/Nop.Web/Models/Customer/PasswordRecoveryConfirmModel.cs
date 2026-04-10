using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Customer;

public class PasswordRecoveryConfirmModel : BaseNopModel
{
    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    public string? ConfirmNewPassword { get; set; }

    public bool DisablePasswordChanging { get; set; }
    public string? Result { get; set; }
}
