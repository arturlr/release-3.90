using Microsoft.AspNetCore.Mvc;

using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Nop.Web.Validators.Customer;

namespace Nop.Web.Models.Customer
{
    
    public partial class PasswordRecoveryModel : BaseNopModel
    {
        
        [NopResourceDisplayName("Account.PasswordRecovery.Email")]
        public string Email { get; set; }

        public string Result { get; set; }
    }
}