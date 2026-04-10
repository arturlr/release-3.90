using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.ShoppingCart;

public class WishlistModel : BaseNopModel
{
    public Guid CustomerGuid { get; set; }
    public string? CustomerFullname { get; set; }
    public bool EmailWishlistEnabled { get; set; }
    public bool ShowSku { get; set; }
    public bool ShowProductImages { get; set; }
    public bool IsEditable { get; set; }
    public bool DisplayAddToCart { get; set; }
    public bool DisplayTaxShippingInfo { get; set; }
    public List<ShoppingCartItemModel> Items { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    public class ShoppingCartItemModel : BaseNopEntityModel
    {
        public string? Sku { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSeName { get; set; }
        public string? UnitPrice { get; set; }
        public string? SubTotal { get; set; }
        public int Quantity { get; set; }
        public string? AttributeInfo { get; set; }
        public string? RecurringInfo { get; set; }
        public string? RentalInfo { get; set; }
        public bool AllowItemEditing { get; set; }
        public List<string> Warnings { get; set; } = [];
    }
}
