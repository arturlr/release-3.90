using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Customer;

public class LoginModel : BaseNopModel
{
    public bool CheckoutAsGuest { get; set; }

    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    public bool UsernamesEnabled { get; set; }
    public string? Username { get; set; }

    [DataType(DataType.Password)]
    public string? Password { get; set; }

    public bool RememberMe { get; set; }
}
