using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ProductReviewController(
    IProductService productService,
    ICustomerService customerService,
    IDateTimeHelper dateTimeHelper,
    IStoreService storeService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    IWorkContext workContext) : BaseAdminController
{
    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageProductReviews"))
            return Forbid();

        var model = new ProductReviewListModel
        {
            IsLoggedInAsVendor = workContext.CurrentVendor is not null
        };

        model.AvailableStores.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var s in await storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });

        model.AvailableApprovedOptions.Add(new SelectListItem { Text = "All", Value = "0" });
        model.AvailableApprovedOptions.Add(new SelectListItem { Text = "Approved only", Value = "1" });
        model.AvailableApprovedOptions.Add(new SelectListItem { Text = "Disapproved only", Value = "2" });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> ProductReviewList(DataSourceRequest command, ProductReviewListModel model)
    {
        if (!permissionService.Authorize("ManageProductReviews"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var vendorId = workContext.CurrentVendor?.Id ?? 0;

        DateTime? fromUtc = model.CreatedOnFrom.HasValue
            ? dateTimeHelper.ConvertToUtcTime(model.CreatedOnFrom.Value, dateTimeHelper.CurrentTimeZone)
            : null;
        DateTime? toUtc = model.CreatedOnTo.HasValue
            ? dateTimeHelper.ConvertToUtcTime(model.CreatedOnTo.Value, dateTimeHelper.CurrentTimeZone).AddDays(1)
            : null;

        bool? approved = model.SearchApprovedId > 0 ? model.SearchApprovedId == 1 : null;

        var reviews = await productService.GetAllProductReviewsAsync(
            0, approved, fromUtc, toUtc, model.SearchText,
            model.SearchStoreId, model.SearchProductId, vendorId,
            command.Page - 1, command.PageSize);

        var stores = (await storeService.GetAllStoresAsync()).ToDictionary(s => s.Id, s => s.Name);
        var gridData = new List<ProductReviewModel>();
        foreach (var r in reviews)
        {
            var product = await productService.GetProductByIdAsync(r.ProductId);
            var customer = await customerService.GetCustomerByIdAsync(r.CustomerId);
            var isRegistered = customer is not null && (await customerService.GetCustomerRoleIdsAsync(customer))
                .Contains((await customerService.GetCustomerRoleBySystemNameAsync("Registered"))?.Id ?? 0);

            gridData.Add(new ProductReviewModel
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = product?.Name,
                CustomerId = r.CustomerId,
                CustomerInfo = isRegistered ? customer!.Email : "Guest",
                Title = r.Title,
                ReviewText = Core.Html.HtmlHelper.FormatText(r.ReviewText ?? string.Empty, false, true, false, false, false, false),
                ReplyText = Core.Html.HtmlHelper.FormatText(r.ReplyText ?? string.Empty, false, true, false, false, false, false),
                Rating = r.Rating,
                IsApproved = r.IsApproved,
                StoreName = stores.GetValueOrDefault(r.StoreId, "Deleted"),
                CreatedOn = dateTimeHelper.ConvertToUserTime(r.CreatedOnUtc, DateTimeKind.Utc),
                IsLoggedInAsVendor = workContext.CurrentVendor is not null
            });
        }

        return Json(new DataSourceResult { Data = gridData, Total = reviews.TotalCount });
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageProductReviews"))
            return Forbid();

        var review = await productService.GetProductReviewByIdAsync(id);
        if (review is null)
            return RedirectToAction("List");

        // vendor access restriction
        if (workContext.CurrentVendor is not null)
        {
            var product = await productService.GetProductByIdAsync(review.ProductId);
            if (product?.VendorId != workContext.CurrentVendor.Id)
                return RedirectToAction("List");
        }

        var model = await PrepareProductReviewModelAsync(review, false);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProductReviewModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageProductReviews"))
            return Forbid();

        var review = await productService.GetProductReviewByIdAsync(model.Id);
        if (review is null)
            return RedirectToAction("List");

        // vendor access restriction
        var isVendor = workContext.CurrentVendor is not null;
        if (isVendor)
        {
            var product = await productService.GetProductByIdAsync(review.ProductId);
            if (product?.VendorId != workContext.CurrentVendor!.Id)
                return RedirectToAction("List");
        }

        if (ModelState.IsValid)
        {
            var previousIsApproved = review.IsApproved;

            // vendor can only edit ReplyText
            if (!isVendor)
            {
                review.Title = model.Title;
                review.ReviewText = model.ReviewText;
                review.IsApproved = model.IsApproved;
            }
            review.ReplyText = model.ReplyText;

            await productService.UpdateProductReviewAsync(review);

            customerActivityService.InsertActivity("EditProductReview", $"Edited a product review (ID = {review.Id})");

            if (!isVendor)
            {
                var product = await productService.GetProductByIdAsync(review.ProductId);
                if (product is not null)
                    await productService.UpdateProductReviewTotalsAsync(product);
            }

            return continueEditing ? RedirectToAction("Edit", new { id = review.Id }) : RedirectToAction("List");
        }

        // redisplay form on validation failure
        var editModel = await PrepareProductReviewModelAsync(review, false);
        return View(editModel);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageProductReviews"))
            return Forbid();

        // vendor cannot delete reviews
        if (workContext.CurrentVendor is not null)
            return RedirectToAction("List");

        var review = await productService.GetProductReviewByIdAsync(id);
        if (review is null)
            return RedirectToAction("List");

        var productId = review.ProductId;
        await productService.DeleteProductReviewAsync(review);

        customerActivityService.InsertActivity("DeleteProductReview", $"Deleted a product review (ID = {id})");

        var product = await productService.GetProductByIdAsync(productId);
        if (product is not null)
            await productService.UpdateProductReviewTotalsAsync(product);

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageProductReviews"))
            return Forbid();

        if (workContext.CurrentVendor is not null)
            return Json(new { Result = true });

        if (selectedIds is not null)
        {
            var reviews = await productService.GetProductReviewsByIdsAsync(selectedIds.ToArray());
            var productIds = reviews.Select(r => r.ProductId).Distinct().ToArray();

            await productService.DeleteProductReviewsAsync(reviews);

            foreach (var pid in productIds)
            {
                var product = await productService.GetProductByIdAsync(pid);
                if (product is not null)
                    await productService.UpdateProductReviewTotalsAsync(product);
            }
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> ApproveSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageProductReviews"))
            return Forbid();

        if (workContext.CurrentVendor is not null)
            return Json(new { Result = true });

        if (selectedIds is not null)
        {
            var reviews = await productService.GetProductReviewsByIdsAsync(selectedIds.ToArray());
            foreach (var r in reviews.Where(r => !r.IsApproved))
            {
                r.IsApproved = true;
                await productService.UpdateProductReviewAsync(r);

                var product = await productService.GetProductByIdAsync(r.ProductId);
                if (product is not null)
                    await productService.UpdateProductReviewTotalsAsync(product);
            }
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> DisapproveSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageProductReviews"))
            return Forbid();

        if (workContext.CurrentVendor is not null)
            return Json(new { Result = true });

        if (selectedIds is not null)
        {
            var reviews = await productService.GetProductReviewsByIdsAsync(selectedIds.ToArray());
            foreach (var r in reviews.Where(r => r.IsApproved))
            {
                r.IsApproved = false;
                await productService.UpdateProductReviewAsync(r);

                var product = await productService.GetProductByIdAsync(r.ProductId);
                if (product is not null)
                    await productService.UpdateProductReviewTotalsAsync(product);
            }
        }

        return Json(new { Result = true });
    }

    public async Task<JsonResult> ProductSearchAutoComplete(string? term)
    {
        if (!permissionService.Authorize("ManageProductReviews"))
            return Json(Array.Empty<object>());

        if (string.IsNullOrWhiteSpace(term) || term.Length < 3)
            return Json(Array.Empty<object>());

        var vendorId = workContext.CurrentVendor?.Id ?? 0;
        var products = await productService.SearchProductsAsync(keywords: term, vendorId: vendorId, pageSize: 15, showHidden: true);

        return Json(products.Select(p => new { label = p.Name, productid = p.Id }));
    }

    #region Helpers

    private async Task<ProductReviewModel> PrepareProductReviewModelAsync(ProductReview review, bool formatText)
    {
        var product = await productService.GetProductByIdAsync(review.ProductId);
        var customer = await customerService.GetCustomerByIdAsync(review.CustomerId);
        var isRegistered = customer is not null && (await customerService.GetCustomerRoleIdsAsync(customer))
            .Contains((await customerService.GetCustomerRoleBySystemNameAsync("Registered"))?.Id ?? 0);
        var stores = (await storeService.GetAllStoresAsync()).ToDictionary(s => s.Id, s => s.Name);

        return new ProductReviewModel
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = product?.Name,
            CustomerId = review.CustomerId,
            CustomerInfo = isRegistered ? customer!.Email : "Guest",
            Title = review.Title,
            ReviewText = formatText
                ? Core.Html.HtmlHelper.FormatText(review.ReviewText ?? string.Empty, false, true, false, false, false, false)
                : review.ReviewText,
            ReplyText = formatText
                ? Core.Html.HtmlHelper.FormatText(review.ReplyText ?? string.Empty, false, true, false, false, false, false)
                : review.ReplyText,
            Rating = review.Rating,
            IsApproved = review.IsApproved,
            StoreName = stores.GetValueOrDefault(review.StoreId, "Deleted"),
            CreatedOn = dateTimeHelper.ConvertToUserTime(review.CreatedOnUtc, DateTimeKind.Utc),
            IsLoggedInAsVendor = workContext.CurrentVendor is not null
        };
    }

    #endregion
}
