using System.Collections.Generic;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Rendering;


namespace Nop.Admin.Models.Orders
{
    public partial class GiftCardListModel : BaseNopModel
    {
        public GiftCardListModel()
        {
            ActivatedList = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Admin.GiftCards.List.CouponCode")]

        public string CouponCode { get; set; }

        [NopResourceDisplayName("Admin.GiftCards.List.RecipientName")]

        public string RecipientName { get; set; }

        [NopResourceDisplayName("Admin.GiftCards.List.Activated")]
        public int ActivatedId { get; set; }
        [NopResourceDisplayName("Admin.GiftCards.List.Activated")]
        public IList<SelectListItem> ActivatedList { get; set; }
    }
}