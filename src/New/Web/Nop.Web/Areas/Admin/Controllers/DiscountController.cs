using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Directory;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Discounts;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class DiscountController(
    IDiscountService discountService,
    ICategoryService categoryService,
    IManufacturerService manufacturerService,
    IProductService productService,
    IOrderService orderService,
    ICurrencyService currencyService,
    IDateTimeHelper dateTimeHelper,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    IPriceFormatter priceFormatter,
    CurrencySettings currencySettings) : BaseAdminController
{
    #region Discounts

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Forbid();

        var model = new DiscountListModel();
        model.AvailableDiscountTypes.Add(new SelectListItem { Text = "All", Value = "0" });
        model.AvailableDiscountTypes.AddRange(GetDiscountTypeSelectList());

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> DiscountList(DataSourceRequest command, DiscountListModel model)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        DiscountType? discountType = model.SearchDiscountTypeId > 0 ? (DiscountType)model.SearchDiscountTypeId : null;
        var discounts = await discountService.GetAllDiscountsAsync(discountType,
            model.SearchDiscountCouponCode, model.SearchDiscountName, showHidden: true);

        var pagedDiscounts = discounts.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize).ToList();

        var gridData = new List<DiscountGridModel>();
        foreach (var d in pagedDiscounts)
        {
            gridData.Add(new DiscountGridModel
            {
                Id = d.Id,
                Name = d.Name,
                DiscountTypeName = GetDiscountTypeName(d.DiscountType),
                DiscountAmount = d.DiscountAmount,
                DiscountPercentage = d.DiscountPercentage,
                UsePercentage = d.UsePercentage,
                TimesUsed = (await discountService.GetAllDiscountUsageHistoryAsync(d.Id, pageSize: 1)).TotalCount
            });
        }

        return Json(new DataSourceResult { Data = gridData, Total = discounts.Count });
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Forbid();

        var model = new DiscountModel { LimitationTimes = 1 };
        await PrepareDiscountModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(DiscountModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var discount = new Discount
            {
                Name = model.Name,
                DiscountTypeId = model.DiscountTypeId,
                UsePercentage = model.UsePercentage,
                DiscountPercentage = model.DiscountPercentage,
                DiscountAmount = model.DiscountAmount,
                MaximumDiscountAmount = model.MaximumDiscountAmount,
                StartDateUtc = model.StartDateUtc,
                EndDateUtc = model.EndDateUtc,
                RequiresCouponCode = model.RequiresCouponCode,
                CouponCode = model.CouponCode,
                IsCumulative = model.IsCumulative,
                DiscountLimitationId = model.DiscountLimitationId,
                LimitationTimes = model.LimitationTimes,
                MaximumDiscountedQuantity = model.MaximumDiscountedQuantity,
                AppliedToSubCategories = model.AppliedToSubCategories
            };
            await discountService.InsertDiscountAsync(discount);

            customerActivityService.InsertActivity("AddNewDiscount", "Added a new discount '{0}'", discount.Name ?? "");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = discount.Id });
            return RedirectToAction("List");
        }

        await PrepareDiscountModelDropdownsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Forbid();

        var discount = await discountService.GetDiscountByIdAsync(id);
        if (discount == null)
            return RedirectToAction("List");

        var model = new DiscountModel
        {
            Id = discount.Id,
            Name = discount.Name,
            DiscountTypeId = discount.DiscountTypeId,
            UsePercentage = discount.UsePercentage,
            DiscountPercentage = discount.DiscountPercentage,
            DiscountAmount = discount.DiscountAmount,
            MaximumDiscountAmount = discount.MaximumDiscountAmount,
            StartDateUtc = discount.StartDateUtc,
            EndDateUtc = discount.EndDateUtc,
            RequiresCouponCode = discount.RequiresCouponCode,
            CouponCode = discount.CouponCode,
            IsCumulative = discount.IsCumulative,
            DiscountLimitationId = discount.DiscountLimitationId,
            LimitationTimes = discount.LimitationTimes,
            MaximumDiscountedQuantity = discount.MaximumDiscountedQuantity,
            AppliedToSubCategories = discount.AppliedToSubCategories,
            TimesUsed = (await discountService.GetAllDiscountUsageHistoryAsync(discount.Id, pageSize: 1)).TotalCount
        };
        await PrepareDiscountModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(DiscountModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Forbid();

        var discount = await discountService.GetDiscountByIdAsync(model.Id);
        if (discount == null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            var prevDiscountType = discount.DiscountType;

            discount.Name = model.Name;
            discount.DiscountTypeId = model.DiscountTypeId;
            discount.UsePercentage = model.UsePercentage;
            discount.DiscountPercentage = model.DiscountPercentage;
            discount.DiscountAmount = model.DiscountAmount;
            discount.MaximumDiscountAmount = model.MaximumDiscountAmount;
            discount.StartDateUtc = model.StartDateUtc;
            discount.EndDateUtc = model.EndDateUtc;
            discount.RequiresCouponCode = model.RequiresCouponCode;
            discount.CouponCode = model.CouponCode;
            discount.IsCumulative = model.IsCumulative;
            discount.DiscountLimitationId = model.DiscountLimitationId;
            discount.LimitationTimes = model.LimitationTimes;
            discount.MaximumDiscountedQuantity = model.MaximumDiscountedQuantity;
            discount.AppliedToSubCategories = model.AppliedToSubCategories;

            await discountService.UpdateDiscountAsync(discount);

            // clean up old entity mappings if discount type changed
            if (prevDiscountType == DiscountType.AssignedToCategories && discount.DiscountType != DiscountType.AssignedToCategories)
                await ClearCategoryMappingsAsync(discount.Id);
            if (prevDiscountType == DiscountType.AssignedToManufacturers && discount.DiscountType != DiscountType.AssignedToManufacturers)
                await ClearManufacturerMappingsAsync(discount.Id);
            if (prevDiscountType == DiscountType.AssignedToSkus && discount.DiscountType != DiscountType.AssignedToSkus)
                await ClearProductMappingsAsync(discount.Id);

            customerActivityService.InsertActivity("EditDiscount", "Edited discount '{0}'", discount.Name ?? "");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = discount.Id });
            return RedirectToAction("List");
        }

        await PrepareDiscountModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Forbid();

        var discount = await discountService.GetDiscountByIdAsync(id);
        if (discount == null)
            return RedirectToAction("List");

        await discountService.DeleteDiscountAsync(discount);

        customerActivityService.InsertActivity("DeleteDiscount", "Deleted discount '{0}'", discount.Name ?? "");

        return RedirectToAction("List");
    }

    #endregion

    #region Applied to products

    [HttpPost]
    public async Task<JsonResult> ProductList(DataSourceRequest command, int discountId)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var productIds = await discountService.GetAppliedProductIdsAsync(discountId);
        var data = new List<AppliedToProductModel>();
        foreach (var pid in productIds.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize))
        {
            var product = await productService.GetProductByIdAsync(pid);
            if (product != null && !product.Deleted)
                data.Add(new AppliedToProductModel { ProductId = product.Id, ProductName = product.Name });
        }

        return Json(new DataSourceResult { Data = data, Total = productIds.Count });
    }

    [HttpPost]
    public async Task<JsonResult> ProductDelete(int discountId, int productId)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new { });

        var mapping = await discountService.GetDiscountProductMappingAsync(discountId, productId);
        if (mapping != null)
            await discountService.DeleteDiscountProductMappingAsync(mapping);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> ProductAdd(int discountId, [FromForm] IEnumerable<int> selectedProductIds)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new { });

        foreach (var productId in selectedProductIds)
        {
            var existing = await discountService.GetDiscountProductMappingAsync(discountId, productId);
            if (existing == null)
                await discountService.InsertDiscountProductMappingAsync(new DiscountProductMapping { DiscountId = discountId, ProductId = productId });
        }

        return Json(new { Result = true });
    }

    #endregion

    #region Applied to categories

    [HttpPost]
    public async Task<JsonResult> CategoryList(DataSourceRequest command, int discountId)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var categoryIds = (await discountService.GetAllDiscountsAsync())
            .Where(_ => false).Select(_ => 0).ToList(); // placeholder — use mapping query
        // query directly from mapping
        var allCategories = await categoryService.GetAllCategoriesAsync(showHidden: true);
        var mappedCategoryIds = await GetMappedCategoryIdsAsync(discountId);
        var data = new List<AppliedToCategoryModel>();
        foreach (var catId in mappedCategoryIds.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize))
        {
            var category = await categoryService.GetCategoryByIdAsync(catId);
            if (category != null && !category.Deleted)
                data.Add(new AppliedToCategoryModel { CategoryId = category.Id, CategoryName = category.Name });
        }

        return Json(new DataSourceResult { Data = data, Total = mappedCategoryIds.Count });
    }

    [HttpPost]
    public async Task<JsonResult> CategoryDelete(int discountId, int categoryId)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new { });

        var mapping = await discountService.GetDiscountCategoryMappingAsync(discountId, categoryId);
        if (mapping != null)
            await discountService.DeleteDiscountCategoryMappingAsync(mapping);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> CategoryAdd(int discountId, [FromForm] IEnumerable<int> selectedCategoryIds)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new { });

        foreach (var categoryId in selectedCategoryIds)
        {
            var existing = await discountService.GetDiscountCategoryMappingAsync(discountId, categoryId);
            if (existing == null)
                await discountService.InsertDiscountCategoryMappingAsync(new DiscountCategoryMapping { DiscountId = discountId, CategoryId = categoryId });
        }

        return Json(new { Result = true });
    }

    #endregion

    #region Applied to manufacturers

    [HttpPost]
    public async Task<JsonResult> ManufacturerList(DataSourceRequest command, int discountId)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var manufacturerIds = await discountService.GetAppliedManufacturerIdsAsync(discountId);
        var data = new List<AppliedToManufacturerModel>();
        foreach (var mfrId in manufacturerIds.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize))
        {
            var manufacturer = await manufacturerService.GetManufacturerByIdAsync(mfrId);
            if (manufacturer != null && !manufacturer.Deleted)
                data.Add(new AppliedToManufacturerModel { ManufacturerId = manufacturer.Id, ManufacturerName = manufacturer.Name });
        }

        return Json(new DataSourceResult { Data = data, Total = manufacturerIds.Count });
    }

    [HttpPost]
    public async Task<JsonResult> ManufacturerDelete(int discountId, int manufacturerId)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new { });

        var mapping = await discountService.GetDiscountManufacturerMappingAsync(discountId, manufacturerId);
        if (mapping != null)
            await discountService.DeleteDiscountManufacturerMappingAsync(mapping);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> ManufacturerAdd(int discountId, [FromForm] IEnumerable<int> selectedManufacturerIds)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new { });

        foreach (var manufacturerId in selectedManufacturerIds)
        {
            var existing = await discountService.GetDiscountManufacturerMappingAsync(discountId, manufacturerId);
            if (existing == null)
                await discountService.InsertDiscountManufacturerMappingAsync(new DiscountManufacturerMapping { DiscountId = discountId, ManufacturerId = manufacturerId });
        }

        return Json(new { Result = true });
    }

    #endregion

    #region Usage history

    [HttpPost]
    public async Task<JsonResult> UsageHistoryList(DataSourceRequest command, int discountId)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var history = await discountService.GetAllDiscountUsageHistoryAsync(discountId, pageIndex: command.Page - 1, pageSize: command.PageSize);

        var data = new List<DiscountUsageHistoryModel>();
        foreach (var duh in history)
        {
            var order = await orderService.GetOrderByIdAsync(duh.OrderId);
            data.Add(new DiscountUsageHistoryModel
            {
                Id = duh.Id,
                DiscountId = duh.DiscountId,
                OrderId = duh.OrderId,
                OrderTotal = order != null ? await priceFormatter.FormatPriceAsync(order.OrderTotal, true, false) : "",
                CreatedOn = dateTimeHelper.ConvertToUserTime(duh.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        return Json(new DataSourceResult { Data = data, Total = history.TotalCount });
    }

    [HttpPost]
    public async Task<JsonResult> UsageHistoryDelete(int id)
    {
        if (!permissionService.Authorize("ManageDiscounts"))
            return Json(new { });

        var duh = await discountService.GetDiscountUsageHistoryByIdAsync(id);
        if (duh != null)
            await discountService.DeleteDiscountUsageHistoryAsync(duh);

        return Json(new { });
    }

    #endregion

    #region Private helpers

    private async Task<IList<int>> GetMappedCategoryIdsAsync(int discountId)
    {
        // Use GetAppliedCategoryIdsAsync but it requires a Customer — use direct mapping query instead
        // We need the raw mapping IDs without subcategory expansion
        var mapping = await discountService.GetDiscountCategoryMappingAsync(discountId, 0);
        // Actually, let's just get all category IDs from the mapping table
        // The service doesn't expose a raw list without customer context, so query via individual checks
        // Better approach: get all categories and check which have mappings
        var allCategories = await categoryService.GetAllCategoriesAsync(showHidden: true);
        var result = new List<int>();
        foreach (var cat in allCategories)
        {
            var m = await discountService.GetDiscountCategoryMappingAsync(discountId, cat.Id);
            if (m != null)
                result.Add(cat.Id);
        }
        return result;
    }

    private async Task ClearCategoryMappingsAsync(int discountId)
    {
        var categoryIds = await GetMappedCategoryIdsAsync(discountId);
        foreach (var catId in categoryIds)
        {
            var mapping = await discountService.GetDiscountCategoryMappingAsync(discountId, catId);
            if (mapping != null)
                await discountService.DeleteDiscountCategoryMappingAsync(mapping);
        }
    }

    private async Task ClearManufacturerMappingsAsync(int discountId)
    {
        var manufacturerIds = await discountService.GetAppliedManufacturerIdsAsync(discountId);
        foreach (var mfrId in manufacturerIds)
        {
            var mapping = await discountService.GetDiscountManufacturerMappingAsync(discountId, mfrId);
            if (mapping != null)
                await discountService.DeleteDiscountManufacturerMappingAsync(mapping);
        }
    }

    private async Task ClearProductMappingsAsync(int discountId)
    {
        var productIds = await discountService.GetAppliedProductIdsAsync(discountId);
        foreach (var pid in productIds)
        {
            var mapping = await discountService.GetDiscountProductMappingAsync(discountId, pid);
            if (mapping != null)
                await discountService.DeleteDiscountProductMappingAsync(mapping);
        }
    }

    #endregion
}
