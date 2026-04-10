using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog;

public class BackInStockSubscribeModel : BaseNopModel
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSeName { get; set; }
    public bool IsCurrentCustomerRegistered { get; set; }
    public bool SubscriptionAllowed { get; set; }
    public bool AlreadySubscribed { get; set; }
    public int MaximumBackInStockSubscriptions { get; set; }
    public int CurrentNumberOfBackInStockSubscriptions { get; set; }
}

public class CustomerBackInStockSubscriptionsModel : BaseNopModel
{
    public List<BackInStockSubscriptionModel> Subscriptions { get; set; } = [];
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    public class BackInStockSubscriptionModel : BaseNopEntityModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? SeName { get; set; }
    }
}
