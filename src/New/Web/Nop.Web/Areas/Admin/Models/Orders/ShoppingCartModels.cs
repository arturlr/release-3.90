namespace Nop.Web.Areas.Admin.Models.Orders;

public class ShoppingCartModel
{
    public int CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public int TotalItems { get; set; }
}

public class ShoppingCartItemModel
{
    public int Id { get; set; }
    public string Store { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string AttributeInfo { get; set; } = string.Empty;
    public string UnitPrice { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Total { get; set; } = string.Empty;
    public DateTime UpdatedOn { get; set; }
}
