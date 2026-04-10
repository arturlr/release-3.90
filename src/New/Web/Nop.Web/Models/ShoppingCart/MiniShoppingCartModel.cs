using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.ShoppingCart;

public class MiniShoppingCartModel : BaseNopModel
{
    public List<ShoppingCartItemModel> Items { get; set; } = [];
    public int TotalProducts { get; set; }
    public string? SubTotal { get; set; }
    public bool DisplayShoppingCartButton { get; set; }
    public bool DisplayCheckoutButton { get; set; }
    public bool CurrentCustomerIsGuest { get; set; }
    public bool AnonymousCheckoutAllowed { get; set; }
    public bool ShowProductImages { get; set; }

    public class ShoppingCartItemModel : BaseNopEntityModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSeName { get; set; }
        public int Quantity { get; set; }
        public string? UnitPrice { get; set; }
        public string? AttributeInfo { get; set; }
    }
}
