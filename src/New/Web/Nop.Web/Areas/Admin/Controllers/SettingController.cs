using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Vendors;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.Settings;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class SettingController(
    ISettingService settingService,
    IStoreService storeService,
    IWorkContext workContext,
    IGenericAttributeService genericAttributeService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    IDateTimeHelper dateTimeHelper,
    IPictureService pictureService) : BaseAdminController
{
    #region Helpers

    private async Task<int> GetActiveStoreScopeAsync()
    {
        var stores = await storeService.GetAllStoresAsync();
        if (stores.Count < 2)
            return 0;

        var storeId = await workContext.CurrentCustomer.GetAttributeAsync<int>(
            "AdminAreaStoreScopeConfiguration", genericAttributeService);
        var store = await storeService.GetStoreByIdAsync(storeId);
        return store?.Id ?? 0;
    }

    public async Task<IActionResult> ChangeStoreScopeConfiguration(int storeid, string returnUrl = "")
    {
        if (storeid > 0)
        {
            var store = await storeService.GetStoreByIdAsync(storeid);
            if (store is null)
                storeid = 0;
        }

        await genericAttributeService.SaveAttributeAsync(
            workContext.CurrentCustomer, "AdminAreaStoreScopeConfiguration", storeid);

        if (!string.IsNullOrEmpty(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Blog");
    }

    #endregion
}
