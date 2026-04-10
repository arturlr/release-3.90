using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Discounts;

public class DiscountListModel : BaseNopModel
{
    public string? SearchDiscountName { get; set; }
    public string? SearchDiscountCouponCode { get; set; }
    public int SearchDiscountTypeId { get; set; }

    public List<SelectListItem> AvailableDiscountTypes { get; set; } = [];
}

public class DiscountModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public int DiscountTypeId { get; set; }
    public string? DiscountTypeName { get; set; }
    public bool UsePercentage { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public bool RequiresCouponCode { get; set; }
    public string? CouponCode { get; set; }
    public bool IsCumulative { get; set; }
    public int DiscountLimitationId { get; set; }
    public int LimitationTimes { get; set; }
    public int? MaximumDiscountedQuantity { get; set; }
    public bool AppliedToSubCategories { get; set; }
    public string? PrimaryStoreCurrencyCode { get; set; }
    public int TimesUsed { get; set; }

    public List<SelectListItem> AvailableDiscountTypes { get; set; } = [];
    public List<SelectListItem> AvailableDiscountLimitationTypes { get; set; } = [];
}

public class DiscountGridModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? DiscountTypeName { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool UsePercentage { get; set; }
    public int TimesUsed { get; set; }
}

public class AppliedToProductModel : BaseNopEntityModel
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
}

public class AppliedToCategoryModel : BaseNopEntityModel
{
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
}

public class AppliedToManufacturerModel : BaseNopEntityModel
{
    public int ManufacturerId { get; set; }
    public string? ManufacturerName { get; set; }
}

public class DiscountUsageHistoryModel : BaseNopEntityModel
{
    public int DiscountId { get; set; }
    public int OrderId { get; set; }
    public string? OrderTotal { get; set; }
    public DateTime CreatedOn { get; set; }
}
