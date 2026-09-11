using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Discounts;
using Nop.Plugin.DiscountRules.CustomerRoles.Models;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Security;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.DiscountRules.CustomerRoles.Controllers
{
    /// <remarks>
    /// Task 10.1 substitutions, all of them reusing decisions tasks 7.3 (§30) and 8.3 (§55)
    /// already recorded — nothing here was re-derived:
    /// <list type="bullet">
    /// <item><c>using System.Web.Mvc;</c> → <c>using Microsoft.AspNetCore.Mvc;</c> plus
    /// <c>Microsoft.AspNetCore.Mvc.Rendering</c> for <see cref="SelectListItem"/>, which is in
    /// a different namespace from the rest of MVC.</item>
    /// <item><c>Json(x, JsonRequestBehavior.AllowGet)</c> → <c>Json(x)</c>.
    /// <c>JsonRequestBehavior</c> does not exist in ASP.NET Core: there is no JSON-hijacking
    /// guard and therefore no opt-out from it. Note that this is the direction that RELAXES —
    /// MVC 5's default was <c>DenyGet</c> and this call site explicitly opted out of it, so
    /// the ported behaviour is what 3.90 asked for (§55.5 made the identical change at 32
    /// admin sites).</item>
    /// <item><c>View("~/Plugins/DiscountRules.CustomerRoles/Views/Configure.cshtml", model)</c>
    /// is <b>UNCHANGED</b>. That is the point of the <c>Content</c>/<c>Link</c> block in the
    /// project file: the compiled Razor identifier is made to be exactly this path, so no
    /// call site had to move. See the long note in
    /// <c>Nop.Plugin.DiscountRules.CustomerRoles.csproj</c>.</item>
    /// </list>
    /// <c>ActionResult</c>, <c>[HttpPost]</c>, <c>Content(...)</c>, <c>ViewData.TemplateInfo</c>
    /// and <c>ViewData.TemplateInfo.HtmlFieldPrefix</c> all exist in ASP.NET Core with the same
    /// shapes and needed no edit. <c>[AdminAuthorize]</c> and <c>[AdminAntiForgery]</c> were
    /// ported in place by tasks 6.4/7.3 and keep their names.
    /// </remarks>
    [AdminAuthorize]
    public class DiscountRulesCustomerRolesController : BasePluginController
    {
        private readonly IDiscountService _discountService;
        private readonly ICustomerService _customerService;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

        public DiscountRulesCustomerRolesController(IDiscountService discountService,
            ICustomerService customerService, ISettingService settingService,
            IPermissionService permissionService)
        {
            this._discountService = discountService;
            this._customerService = customerService;
            this._settingService = settingService;
            this._permissionService = permissionService;
        }

        public ActionResult Configure(int discountId, int? discountRequirementId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            DiscountRequirement discountRequirement = null;
            if (discountRequirementId.HasValue)
            {
                discountRequirement = discount.DiscountRequirements.FirstOrDefault(dr => dr.Id == discountRequirementId.Value);
                if (discountRequirement == null)
                    return Content("Failed to load requirement.");
            }

            var restrictedToCustomerRoleId = _settingService.GetSettingByKey<int>(string.Format("DiscountRequirement.MustBeAssignedToCustomerRole-{0}", discountRequirementId.HasValue ? discountRequirementId.Value : 0));
            
            var model = new RequirementModel();
            model.RequirementId = discountRequirementId.HasValue ? discountRequirementId.Value : 0;
            model.DiscountId = discountId;
            model.CustomerRoleId = restrictedToCustomerRoleId;
            //customer roles
            //TODO localize "Select customer role"
            model.AvailableCustomerRoles.Add(new SelectListItem { Text = "Select customer role", Value = "0" });
            foreach (var cr in _customerService.GetAllCustomerRoles(true))
                model.AvailableCustomerRoles.Add(new SelectListItem { Text = cr.Name, Value = cr.Id.ToString(), Selected = discountRequirement != null && cr.Id == restrictedToCustomerRoleId });

            //add a prefix
            ViewData.TemplateInfo.HtmlFieldPrefix = string.Format("DiscountRulesCustomerRoles{0}", discountRequirementId.HasValue ? discountRequirementId.Value.ToString() : "0");

            return View("~/Plugins/DiscountRules.CustomerRoles/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult Configure(int discountId, int? discountRequirementId, int customerRoleId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageDiscounts))
                return Content("Access denied");

            var discount = _discountService.GetDiscountById(discountId);
            if (discount == null)
                throw new ArgumentException("Discount could not be loaded");

            DiscountRequirement discountRequirement = null;
            if (discountRequirementId.HasValue)
                discountRequirement = discount.DiscountRequirements.FirstOrDefault(dr => dr.Id == discountRequirementId.Value);

            if (discountRequirement != null)
            {
                //update existing rule
                _settingService.SetSetting(string.Format("DiscountRequirement.MustBeAssignedToCustomerRole-{0}", discountRequirement.Id), customerRoleId);
            }
            else
            {
                //save new rule
                discountRequirement = new DiscountRequirement
                {
                    DiscountRequirementRuleSystemName = "DiscountRequirement.MustBeAssignedToCustomerRole"
                };
                discount.DiscountRequirements.Add(discountRequirement);
                _discountService.UpdateDiscount(discount);

                _settingService.SetSetting(string.Format("DiscountRequirement.MustBeAssignedToCustomerRole-{0}", discountRequirement.Id), customerRoleId);
            }
            //task 10.1: JsonRequestBehavior.AllowGet dropped - see the remarks on this class
            return Json(new { Result = true, NewRequirementId = discountRequirement.Id });
        }
    }
}
