using System.Collections.Generic;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Rendering;


namespace Nop.Admin.Models.Discounts
{
    public partial class DiscountListModel : BaseNopModel
    {
        public DiscountListModel()
        {
            AvailableDiscountTypes = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Admin.Promotions.Discounts.List.SearchDiscountCouponCode")]

        public string SearchDiscountCouponCode { get; set; }

        [NopResourceDisplayName("Admin.Promotions.Discounts.List.SearchDiscountName")]

        public string SearchDiscountName { get; set; }

        [NopResourceDisplayName("Admin.Promotions.Discounts.List.SearchDiscountType")]
        public int SearchDiscountTypeId { get; set; }
        public IList<SelectListItem> AvailableDiscountTypes { get; set; }
    }
}