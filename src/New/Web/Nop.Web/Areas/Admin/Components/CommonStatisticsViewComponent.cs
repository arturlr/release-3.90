using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Web.Areas.Admin.Models.Home;

namespace Nop.Web.Areas.Admin.Components;

public class CommonStatisticsViewComponent(
    IOrderService orderService,
    ICustomerService customerService,
    IReturnRequestService returnRequestService,
    IProductService productService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(
            SystemCustomerRoleNames.Registered);

        var model = new CommonStatisticsModel
        {
            NumberOfOrders = (await orderService.SearchOrdersAsync(pageSize: 1)).TotalCount,
            NumberOfCustomers = registeredRole is null
                ? 0
                : (await customerService.GetAllCustomersAsync(
                    customerRoleIds: [registeredRole.Id], pageSize: 1)).TotalCount,
            NumberOfPendingReturnRequests = (await returnRequestService.SearchReturnRequestsAsync(
                rs: ReturnRequestStatus.Pending, pageSize: 1)).TotalCount,
            NumberOfLowStockProducts =
                (await productService.GetLowStockProductsAsync(pageSize: 1)).TotalCount +
                (await productService.GetLowStockProductCombinationsAsync(pageSize: 1)).TotalCount
        };

        return View(model);
    }
}
