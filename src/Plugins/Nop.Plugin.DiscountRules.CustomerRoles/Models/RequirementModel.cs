using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework;

namespace Nop.Plugin.DiscountRules.CustomerRoles.Models
{
    public class RequirementModel
    {
        public RequirementModel()
        {
            AvailableCustomerRoles = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Plugins.DiscountRules.CustomerRoles.Fields.CustomerRole")]
        public int CustomerRoleId { get; set; }

        public int DiscountId { get; set; }

        public int RequirementId { get; set; }

        //task 10.1: System.Web.Mvc.SelectListItem -> Microsoft.AspNetCore.Mvc.Rendering.SelectListItem.
        //Namespace change only - Text/Value/Selected are unchanged, and Nop.Services'
        //ToSelectList/SelectionIsNotPossible helpers already moved to the same type
        //(runtime-deferrals.md sections 9b and 30).
        public IList<SelectListItem> AvailableCustomerRoles { get; set; }
    }
}
