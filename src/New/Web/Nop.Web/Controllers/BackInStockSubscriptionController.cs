using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Seo;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Controllers;

public class BackInStockSubscriptionController(
    IProductService productService,
    IBackInStockSubscriptionService backInStockSubscriptionService,
    ICustomerService customerService,
    ILocalizationService localizationService,
    IWorkContext workContext,
    IStoreContext storeContext,
    CatalogSettings catalogSettings,
    CustomerSettings customerSettings) : BasePublicController
{
    private async Task<bool> IsRegisteredAsync(Customer customer)
    {
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        return registeredRole != null && roleIds.Contains(registeredRole.Id);
    }

    // GET: /BackInStockSubscription/SubscribePopup?productId=123
    public async Task<IActionResult> SubscribePopup(int productId)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted)
            return Content("");

        var customer = workContext.CurrentCustomer;
        var storeId = storeContext.CurrentStore.Id;

        var model = new BackInStockSubscribeModel
        {
            ProductId = product.Id,
            ProductName = product.Name,
            ProductSeName = SeoExtensions.GetSeName(product.Name ?? string.Empty, false, false),
            IsCurrentCustomerRegistered = await IsRegisteredAsync(customer),
            MaximumBackInStockSubscriptions = catalogSettings.MaximumBackInStockSubscriptions,
            CurrentNumberOfBackInStockSubscriptions = (await backInStockSubscriptionService
                .GetAllSubscriptionsByCustomerIdAsync(customer.Id, storeId, 0, 1)).TotalCount
        };

        if (product.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStock
            && product.BackorderModeId == (int)BackorderMode.NoBackorders
            && product.AllowBackInStockSubscriptions
            && product.StockQuantity <= 0)
        {
            model.SubscriptionAllowed = true;
            model.AlreadySubscribed = await backInStockSubscriptionService
                .FindSubscriptionAsync(customer.Id, product.Id, storeId) != null;
        }

        return PartialView(model);
    }

    // POST: /BackInStockSubscription/SubscribePopupPOST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubscribePopupPOST(int productId)
    {
        var product = await productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted)
            return Json(new { result = "Product not found." });

        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Json(new { result = await localizationService.GetResourceAsync("BackInStockSubscriptions.OnlyRegistered") });

        if (product.ManageInventoryMethodId != (int)ManageInventoryMethod.ManageStock
            || product.BackorderModeId != (int)BackorderMode.NoBackorders
            || !product.AllowBackInStockSubscriptions
            || product.StockQuantity > 0)
        {
            return Json(new { result = await localizationService.GetResourceAsync("BackInStockSubscriptions.NotAllowed") });
        }

        var storeId = storeContext.CurrentStore.Id;
        var existing = await backInStockSubscriptionService.FindSubscriptionAsync(customer.Id, product.Id, storeId);
        if (existing != null)
        {
            await backInStockSubscriptionService.DeleteSubscriptionAsync(existing);
            return Json(new { result = "Unsubscribed" });
        }

        // Check max subscriptions
        var currentCount = (await backInStockSubscriptionService
            .GetAllSubscriptionsByCustomerIdAsync(customer.Id, storeId, 0, 1)).TotalCount;
        if (currentCount >= catalogSettings.MaximumBackInStockSubscriptions)
        {
            return Json(new
            {
                result = string.Format(
                    await localizationService.GetResourceAsync("BackInStockSubscriptions.MaxSubscriptions"),
                    catalogSettings.MaximumBackInStockSubscriptions)
            });
        }

        await backInStockSubscriptionService.InsertSubscriptionAsync(new BackInStockSubscription
        {
            CustomerId = customer.Id,
            ProductId = product.Id,
            StoreId = storeId,
            CreatedOnUtc = DateTime.UtcNow
        });

        return Json(new { result = "Subscribed" });
    }

    // GET: /BackInStockSubscription/CustomerSubscriptions
    public async Task<IActionResult> CustomerSubscriptions(int? page)
    {
        if (customerSettings.HideBackInStockSubscriptionsTab)
            return RedirectToAction("Info", "Customer");

        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        var pageIndex = (page ?? 1) - 1;
        const int pageSize = 10;
        var storeId = storeContext.CurrentStore.Id;

        var list = await backInStockSubscriptionService
            .GetAllSubscriptionsByCustomerIdAsync(customer.Id, storeId, pageIndex, pageSize);

        var model = new CustomerBackInStockSubscriptionsModel
        {
            PageIndex = list.PageIndex,
            PageSize = list.PageSize,
            TotalCount = list.TotalCount
        };

        foreach (var subscription in list)
        {
            var product = await productService.GetProductByIdAsync(subscription.ProductId);
            if (product == null) continue;

            model.Subscriptions.Add(new CustomerBackInStockSubscriptionsModel.BackInStockSubscriptionModel
            {
                Id = subscription.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                SeName = SeoExtensions.GetSeName(product.Name ?? string.Empty, false, false)
            });
        }

        return View(model);
    }

    // POST: /BackInStockSubscription/DeleteSelected
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected([FromForm] IEnumerable<int> subscriptionIds)
    {
        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        foreach (var id in subscriptionIds)
        {
            var subscription = await backInStockSubscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription != null && subscription.CustomerId == customer.Id)
                await backInStockSubscriptionService.DeleteSubscriptionAsync(subscription);
        }

        return RedirectToAction(nameof(CustomerSubscriptions));
    }
}
