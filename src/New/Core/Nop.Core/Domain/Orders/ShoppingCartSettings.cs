using Nop.Core.Configuration;

namespace Nop.Core.Domain.Orders;

public class ShoppingCartSettings : ISettings
{
    public bool DisplayCartAfterAddingProduct { get; set; }
    public bool DisplayWishlistAfterAddingProduct { get; set; }
    public int MaximumShoppingCartItems { get; set; }
    public int MaximumWishlistItems { get; set; }
    public bool AllowOutOfStockItemsToBeAddedToWishlist { get; set; }
    public bool MoveItemsFromWishlistToCart { get; set; }
    public bool CartsSharedBetweenStores { get; set; }
    public bool ShowProductImagesOnShoppingCart { get; set; }
    public bool ShowProductImagesOnWishList { get; set; }
    public bool ShowDiscountBox { get; set; }
    public bool ShowGiftCardBox { get; set; }
    public int CrossSellsNumber { get; set; }
    public bool EmailWishlistEnabled { get; set; }
    public bool AllowAnonymousUsersToEmailWishlist { get; set; }
    public bool MiniShoppingCartEnabled { get; set; }
    public bool ShowProductImagesInMiniShoppingCart { get; set; }
    public int MiniShoppingCartProductNumber { get; set; }
    public bool RoundPricesDuringCalculation { get; set; }
    public bool GroupTierPricesForDistinctShoppingCartItems { get; set; }
    public bool AllowCartItemEditing { get; set; }
    public bool RenderAssociatedAttributeValueQuantity { get; set; }
}
