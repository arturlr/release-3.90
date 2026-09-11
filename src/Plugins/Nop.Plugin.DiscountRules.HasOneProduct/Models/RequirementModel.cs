using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.DiscountRules.HasOneProduct.Models
{
    public class RequirementModel
    {
        [NopResourceDisplayName("Plugins.DiscountRules.HasOneProduct.Fields.Products")]
        public string Products { get; set; }

        public int DiscountId { get; set; }

        public int RequirementId { get; set; }

        #region Nested classes

        public partial class AddProductModel : BaseNopModel
        {
            public AddProductModel()
            {
                AvailableCategories = new List<SelectListItem>();
                AvailableManufacturers = new List<SelectListItem>();
                AvailableStores = new List<SelectListItem>();
                AvailableVendors = new List<SelectListItem>();
                AvailableProductTypes = new List<SelectListItem>();
            }

            //TASK 10.2: [AllowHtml] DELETED - and it is a SECURITY-RELEVANT RELAXATION, recorded
            //rather than glossed over. System.Web.Mvc.AllowHtmlAttribute existed only to opt a
            //property out of ASP.NET request validation, which does not exist in ASP.NET Core
            //(deferral 7.3-3): there is no framework-level "potentially dangerous input" check to
            //opt out OF, so every property now behaves as if it carried [AllowHtml]. Tasks 7.3
            //and 8.3 made exactly this deletion at 99 and 395 sites respectively; this is the
            //same change, once. Nothing is added to compensate - the mechanism has no counterpart.
            [NopResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]
            public string SearchProductName { get; set; }
            [NopResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
            public int SearchCategoryId { get; set; }
            [NopResourceDisplayName("Admin.Catalog.Products.List.SearchManufacturer")]
            public int SearchManufacturerId { get; set; }
            [NopResourceDisplayName("Admin.Catalog.Products.List.SearchStore")]
            public int SearchStoreId { get; set; }
            [NopResourceDisplayName("Admin.Catalog.Products.List.SearchVendor")]
            public int SearchVendorId { get; set; }
            [NopResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
            public int SearchProductTypeId { get; set; }

            //task 10.2: System.Web.Mvc.SelectListItem -> Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            public IList<SelectListItem> AvailableCategories { get; set; }
            public IList<SelectListItem> AvailableManufacturers { get; set; }
            public IList<SelectListItem> AvailableStores { get; set; }
            public IList<SelectListItem> AvailableVendors { get; set; }
            public IList<SelectListItem> AvailableProductTypes { get; set; }

            //vendor
            public bool IsLoggedInAsVendor { get; set; }
        }

        public partial class ProductModel : BaseNopEntityModel
        {
            public string Name { get; set; }

            public bool Published { get; set; }
        }

        #endregion
    }
}
