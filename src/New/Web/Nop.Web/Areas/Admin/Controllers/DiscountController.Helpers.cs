using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Discounts;
using Nop.Web.Areas.Admin.Models.Discounts;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class DiscountController
{
    private static string GetDiscountTypeName(DiscountType type) => type switch
    {
        DiscountType.AssignedToOrderTotal => "Assigned to order total",
        DiscountType.AssignedToSkus => "Assigned to products",
        DiscountType.AssignedToCategories => "Assigned to categories",
        DiscountType.AssignedToManufacturers => "Assigned to manufacturers",
        DiscountType.AssignedToShipping => "Assigned to shipping",
        DiscountType.AssignedToOrderSubTotal => "Assigned to order subtotal",
        _ => type.ToString()
    };

    private static List<SelectListItem> GetDiscountTypeSelectList()
    {
        return Enum.GetValues<DiscountType>()
            .Select(t => new SelectListItem { Text = GetDiscountTypeName(t), Value = ((int)t).ToString() })
            .ToList();
    }

    private static List<SelectListItem> GetDiscountLimitationTypeSelectList()
    {
        return
        [
            new() { Text = "Unlimited", Value = ((int)DiscountLimitationType.Unlimited).ToString() },
            new() { Text = "N times only", Value = ((int)DiscountLimitationType.NTimesOnly).ToString() },
            new() { Text = "N times per customer", Value = ((int)DiscountLimitationType.NTimesPerCustomer).ToString() },
        ];
    }

    private async Task<string> GetPrimaryCurrencyCodeAsync()
    {
        var currency = await currencyService.GetCurrencyByIdAsync(currencySettings.PrimaryStoreCurrencyId);
        return currency?.CurrencyCode ?? "USD";
    }

    private async Task PrepareDiscountModelDropdownsAsync(DiscountModel model)
    {
        model.AvailableDiscountTypes = GetDiscountTypeSelectList();
        model.AvailableDiscountLimitationTypes = GetDiscountLimitationTypeSelectList();
        model.PrimaryStoreCurrencyCode = await GetPrimaryCurrencyCodeAsync();
    }
}
