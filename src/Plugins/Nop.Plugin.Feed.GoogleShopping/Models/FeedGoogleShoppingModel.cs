using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Feed.GoogleShopping.Models
{
    /// <remarks>
    /// Task 11.2. One substitution: <c>using System.Web.Mvc;</c> →
    /// <c>using Microsoft.AspNetCore.Mvc.Rendering;</c>, which is where
    /// <see cref="SelectListItem"/> lives in ASP.NET Core (§30). Nothing else changes —
    /// <c>BaseNopModel</c> and <c>[NopResourceDisplayName]</c> were ported in place by task 6.2
    /// with their names intact, and the three <c>IList&lt;SelectListItem&gt;</c> properties are
    /// populated by the controller and consumed by <c>Html.NopDropDownListFor</c>, both of which
    /// bind the same type.
    /// </remarks>
    public class FeedGoogleShoppingModel
    {
        public FeedGoogleShoppingModel()
        {
            AvailableStores = new List<SelectListItem>();
            AvailableCurrencies = new List<SelectListItem>();
            AvailableGoogleCategories = new List<SelectListItem>();
            GeneratedFiles = new List<GeneratedFileModel>();
        }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.ProductPictureSize")]
        public int ProductPictureSize { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Store")]
        public int StoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Currency")]
        public int CurrencyId { get; set; }
        public IList<SelectListItem> AvailableCurrencies { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.DefaultGoogleCategory")]
        public string DefaultGoogleCategory { get; set; }
        public IList<SelectListItem> AvailableGoogleCategories { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.PassShippingInfoWeight")]
        public bool PassShippingInfoWeight { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.PassShippingInfoDimensions")]
        public bool PassShippingInfoDimensions { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.PricesConsiderPromotions")]
        public bool PricesConsiderPromotions { get; set; }

        [NopResourceDisplayName("Plugins.Feed.GoogleShopping.StaticFilePath")]
        public IList<GeneratedFileModel> GeneratedFiles { get; set; }
        
        public class GeneratedFileModel : BaseNopModel
        {
            public string StoreName { get; set; }
            public string FileUrl { get; set; }
        }

        public class GoogleProductModel : BaseNopModel
        {
            public int ProductId { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.ProductName")]
            public string ProductName { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.GoogleCategory")]
            public string GoogleCategory { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.Gender")]
            public string Gender { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.AgeGroup")]
            public string AgeGroup { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.Color")]
            public string Color { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.Size")]
            public string GoogleSize { get; set; }

            [NopResourceDisplayName("Plugins.Feed.GoogleShopping.Products.CustomGoods")]
            public bool CustomGoods { get; set; }
        }
    }
}