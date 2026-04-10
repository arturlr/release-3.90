using Nop.Core.Domain.Catalog;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.ShoppingCart;

public class ShoppingCartModel : BaseNopModel
{
    public bool ShowSku { get; set; }
    public bool ShowProductImages { get; set; }
    public bool IsEditable { get; set; }
    public List<ShoppingCartItemModel> Items { get; set; } = [];
    public List<CheckoutAttributeModel> CheckoutAttributes { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public string? MinOrderSubtotalWarning { get; set; }
    public bool DisplayTaxShippingInfo { get; set; }
    public bool TermsOfServiceOnShoppingCartPage { get; set; }
    public DiscountBoxModel DiscountBox { get; set; } = new();
    public GiftCardBoxModel GiftCardBox { get; set; } = new();
    public bool HideCheckoutButton { get; set; }
    public bool OnePageCheckoutEnabled { get; set; }

    public class ShoppingCartItemModel : BaseNopEntityModel
    {
        public string? Sku { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSeName { get; set; }
        public string? UnitPrice { get; set; }
        public string? SubTotal { get; set; }
        public string? Discount { get; set; }
        public int Quantity { get; set; }
        public string? AttributeInfo { get; set; }
        public string? RecurringInfo { get; set; }
        public string? RentalInfo { get; set; }
        public bool AllowItemEditing { get; set; }
        public bool DisableRemoval { get; set; }
        public List<string> Warnings { get; set; } = [];
    }

    public class CheckoutAttributeModel : BaseNopEntityModel
    {
        public string? Name { get; set; }
        public string? DefaultValue { get; set; }
        public string? TextPrompt { get; set; }
        public bool IsRequired { get; set; }
        public int? SelectedDay { get; set; }
        public int? SelectedMonth { get; set; }
        public int? SelectedYear { get; set; }
        public List<string> AllowedFileExtensions { get; set; } = [];
        public AttributeControlType AttributeControlType { get; set; }
        public List<CheckoutAttributeValueModel> Values { get; set; } = [];
    }

    public class CheckoutAttributeValueModel : BaseNopEntityModel
    {
        public string? Name { get; set; }
        public string? ColorSquaresRgb { get; set; }
        public string? PriceAdjustment { get; set; }
        public bool IsPreSelected { get; set; }
    }

    public class DiscountBoxModel : BaseNopModel
    {
        public List<DiscountInfoModel> AppliedDiscountsWithCodes { get; set; } = [];
        public bool Display { get; set; }
        public List<string> Messages { get; set; } = [];
        public bool IsApplied { get; set; }

        public class DiscountInfoModel : BaseNopEntityModel
        {
            public string? CouponCode { get; set; }
        }
    }

    public class GiftCardBoxModel : BaseNopModel
    {
        public bool Display { get; set; }
        public string? Message { get; set; }
        public bool IsApplied { get; set; }
    }
}
