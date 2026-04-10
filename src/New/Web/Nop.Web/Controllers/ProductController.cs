using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Controllers;

public partial class ProductController(
    IProductService productService,
    IProductTemplateService productTemplateService,
    IPriceCalculationService priceCalculationService,
    IPriceFormatter priceFormatter,
    IPictureService pictureService,
    IUrlRecordService urlRecordService,
    IRecentlyViewedProductsService recentlyViewedProductsService,
    ICompareProductsService compareProductsService,
    IOrderService orderService,
    ICustomerService customerService,
    IWorkContext workContext,
    IStoreContext storeContext,
    ILocalizationService localizationService,
    IWorkflowMessageService workflowMessageService,
    ICustomerActivityService customerActivityService,
    IAclService aclService,
    IStoreMappingService storeMappingService,
    IPermissionService permissionService,
    CatalogSettings catalogSettings,
    MediaSettings mediaSettings) : BasePublicController
{
    // ── Product Details ──

    public async Task<IActionResult> ProductDetails(int productId)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted)
            return NotFound();

        var notAvailable =
            (!product.Published && !catalogSettings.AllowViewUnpublishedProductPage) ||
            !aclService.Authorize(product) ||
            !await storeMappingService.AuthorizeAsync(product) ||
            !IsAvailable(product);

        if (notAvailable && !permissionService.Authorize(StandardPermissionProvider.ManageProducts))
            return NotFound();

        if (!product.VisibleIndividually)
        {
            var parent = await productService.GetProductByIdAsync(product.ParentGroupedProductId);
            if (parent == null)
                return RedirectToAction("Index", "Home");
            var parentSeName = await parent.GetSeNameAsync(0, urlRecordService);
            return RedirectToAction(nameof(ProductDetails), new { productId = parent.Id, SeName = parentSeName });
        }

        recentlyViewedProductsService.AddProductToRecentlyViewedList(product.Id);

        customerActivityService.InsertActivity("PublicStore.ViewProduct",
            await localizationService.GetResourceAsync("ActivityLog.PublicStore.ViewProduct"), product.Name ?? "");

        var model = await PrepareProductDetailsModelAsync(product);
        return View(model.ProductTemplateViewPath ?? "ProductDetails", model);
    }

    // ── Recently Viewed Products ──

    public async Task<IActionResult> RecentlyViewedProducts()
    {
        if (!catalogSettings.RecentlyViewedProductsEnabled)
            return RedirectToAction("Index", "Home");

        var products = await recentlyViewedProductsService.GetRecentlyViewedProductsAsync(
            catalogSettings.RecentlyViewedProductsNumber);

        var model = new List<ProductOverviewModel>();
        foreach (var p in products)
        {
            if (!IsAvailable(p)) continue;
            if (!aclService.Authorize(p) || !await storeMappingService.AuthorizeAsync(p)) continue;
            model.Add(await PrepareProductOverviewModelAsync(p));
        }

        return View(model);
    }

    // ── New Products ──

    public async Task<IActionResult> NewProducts()
    {
        if (!catalogSettings.NewProductsEnabled)
            return RedirectToAction("Index", "Home");

        var products = await productService.SearchProductsAsync(
            storeId: storeContext.CurrentStore.Id,
            visibleIndividuallyOnly: true,
            markedAsNewOnly: true,
            orderBy: ProductSortingEnum.CreatedOn,
            pageSize: catalogSettings.NewProductsNumber);

        var model = new List<ProductOverviewModel>();
        foreach (var p in products)
            model.Add(await PrepareProductOverviewModelAsync(p));

        return View(model);
    }

    // ── Product Reviews ──

    public async Task<IActionResult> ProductReviews(int productId)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted || !product.Published || !product.AllowCustomerReviews)
            return RedirectToAction("Index", "Home");

        var model = await PrepareProductReviewsModelAsync(product);

        if (await IsGuestAsync(workContext.CurrentCustomer) && !catalogSettings.AllowAnonymousUsersToReviewProduct)
            ModelState.AddModelError("", await localizationService.GetResourceAsync("Reviews.OnlyRegisteredUsersCanWriteReviews"));

        if (catalogSettings.ProductReviewPossibleOnlyAfterPurchasing)
        {
            var orders = await orderService.SearchOrdersAsync(
                customerId: workContext.CurrentCustomer.Id,
                productId: productId,
                osIds: [(int)OrderStatus.Complete],
                pageSize: 1);
            if (orders.Count == 0)
                ModelState.AddModelError("", await localizationService.GetResourceAsync("Reviews.ProductReviewPossibleOnlyAfterPurchasing"));
        }

        model.AddProductReview.Rating = catalogSettings.DefaultProductRatingValue;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductReviewsAdd(int productId, ProductReviewsModel model)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted || !product.Published || !product.AllowCustomerReviews)
            return RedirectToAction("Index", "Home");

        if (await IsGuestAsync(workContext.CurrentCustomer) && !catalogSettings.AllowAnonymousUsersToReviewProduct)
            ModelState.AddModelError("", await localizationService.GetResourceAsync("Reviews.OnlyRegisteredUsersCanWriteReviews"));

        if (catalogSettings.ProductReviewPossibleOnlyAfterPurchasing)
        {
            var orders = await orderService.SearchOrdersAsync(
                customerId: workContext.CurrentCustomer.Id,
                productId: productId,
                osIds: [(int)OrderStatus.Complete],
                pageSize: 1);
            if (orders.Count == 0)
                ModelState.AddModelError("", await localizationService.GetResourceAsync("Reviews.ProductReviewPossibleOnlyAfterPurchasing"));
        }

        if (ModelState.IsValid)
        {
            var rating = model.AddProductReview.Rating;
            if (rating < 1 || rating > 5)
                rating = catalogSettings.DefaultProductRatingValue;

            var productReview = new ProductReview
            {
                ProductId = product.Id,
                CustomerId = workContext.CurrentCustomer.Id,
                Title = model.AddProductReview.Title,
                ReviewText = model.AddProductReview.ReviewText,
                Rating = rating,
                HelpfulYesTotal = 0,
                HelpfulNoTotal = 0,
                IsApproved = !catalogSettings.ProductReviewsMustBeApproved,
                CreatedOnUtc = DateTime.UtcNow,
                StoreId = storeContext.CurrentStore.Id,
            };
            await productService.InsertProductReviewAsync(productReview);
            await productService.UpdateProductReviewTotalsAsync(product);

            if (catalogSettings.NotifyStoreOwnerAboutNewProductReviews)
                await workflowMessageService.SendProductReviewNotificationMessageAsync(productReview, 0);

            customerActivityService.InsertActivity("PublicStore.AddProductReview",
                await localizationService.GetResourceAsync("ActivityLog.PublicStore.AddProductReview"), product.Name ?? "");

            var resultModel = await PrepareProductReviewsModelAsync(product);
            resultModel.AddProductReview.Title = null;
            resultModel.AddProductReview.ReviewText = null;
            resultModel.AddProductReview.SuccessfullyAdded = true;
            resultModel.AddProductReview.Result = productReview.IsApproved
                ? await localizationService.GetResourceAsync("Reviews.SuccessfullyAdded")
                : await localizationService.GetResourceAsync("Reviews.SeeAfterApproving");

            return View("ProductReviews", resultModel);
        }

        var redisplayModel = await PrepareProductReviewsModelAsync(product);
        return View("ProductReviews", redisplayModel);
    }

    [HttpPost]
    public async Task<IActionResult> SetProductReviewHelpfulness(int productReviewId, bool washelpful)
    {
        var productReview = await productService.GetProductReviewByIdAsync(productReviewId);
        if (productReview == null)
            return Json(new { Result = "No product review found", TotalYes = 0, TotalNo = 0 });

        if (await IsGuestAsync(workContext.CurrentCustomer) && !catalogSettings.AllowAnonymousUsersToReviewProduct)
            return Json(new
            {
                Result = await localizationService.GetResourceAsync("Reviews.Helpfulness.OnlyRegistered"),
                TotalYes = productReview.HelpfulYesTotal,
                TotalNo = productReview.HelpfulNoTotal,
            });

        if (productReview.CustomerId == workContext.CurrentCustomer.Id)
            return Json(new
            {
                Result = await localizationService.GetResourceAsync("Reviews.Helpfulness.YourOwnReview"),
                TotalYes = productReview.HelpfulYesTotal,
                TotalNo = productReview.HelpfulNoTotal,
            });

        await productService.SetProductReviewHelpfulnessAsync(productReview, workContext.CurrentCustomer.Id, washelpful);

        return Json(new
        {
            Result = await localizationService.GetResourceAsync("Reviews.Helpfulness.SuccessfullyVoted"),
            TotalYes = productReview.HelpfulYesTotal,
            TotalNo = productReview.HelpfulNoTotal,
        });
    }

    // ── Compare Products ──

    [HttpPost]
    public async Task<IActionResult> AddProductToCompareList(int productId)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted || !product.Published)
            return Json(new { success = false, message = "No product found with the specified ID" });

        if (!catalogSettings.CompareProductsEnabled)
            return Json(new { success = false, message = "Product comparison is disabled" });

        compareProductsService.AddProductToCompareList(productId);

        customerActivityService.InsertActivity("PublicStore.AddToCompareList",
            await localizationService.GetResourceAsync("ActivityLog.PublicStore.AddToCompareList"), product.Name ?? "");

        return Json(new
        {
            success = true,
            message = string.Format(
                await localizationService.GetResourceAsync("Products.ProductHasBeenAddedToCompareList.Link"),
                Url.Action(nameof(CompareProducts))),
        });
    }

    public async Task<IActionResult> RemoveProductFromCompareList(int productId)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null || !catalogSettings.CompareProductsEnabled)
            return RedirectToAction("Index", "Home");

        compareProductsService.RemoveProductFromCompareList(productId);
        return RedirectToAction(nameof(CompareProducts));
    }

    public async Task<IActionResult> CompareProducts()
    {
        if (!catalogSettings.CompareProductsEnabled)
            return RedirectToAction("Index", "Home");

        var model = new CompareProductsModel
        {
            IncludeShortDescriptionInCompareProducts = catalogSettings.IncludeShortDescriptionInCompareProducts,
            IncludeFullDescriptionInCompareProducts = catalogSettings.IncludeFullDescriptionInCompareProducts,
        };

        var products = await compareProductsService.GetComparedProductsAsync();
        foreach (var p in products)
        {
            if (!aclService.Authorize(p) || !await storeMappingService.AuthorizeAsync(p)) continue;
            if (!IsAvailable(p)) continue;
            model.Products.Add(await PrepareProductOverviewModelAsync(p));
        }

        return View(model);
    }

    public IActionResult ClearCompareList()
    {
        if (!catalogSettings.CompareProductsEnabled)
            return RedirectToAction("Index", "Home");

        compareProductsService.ClearCompareProducts();
        return RedirectToAction(nameof(CompareProducts));
    }
}
