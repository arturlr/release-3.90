using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Common;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Affiliates;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class AffiliateController(
    IAffiliateService affiliateService,
    IAddressService addressService,
    ICountryService countryService,
    IStateProvinceService stateProvinceService,
    IOrderService orderService,
    ICustomerService customerService,
    IDateTimeHelper dateTimeHelper,
    IPriceFormatter priceFormatter,
    IWebHelper webHelper,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Affiliates

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageAffiliates"))
            return Forbid();

        return View(new AffiliateListModel());
    }

    [HttpPost]
    public async Task<JsonResult> AffiliateList(DataSourceRequest command, AffiliateListModel model)
    {
        if (!permissionService.Authorize("ManageAffiliates"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var affiliates = await affiliateService.GetAllAffiliatesAsync(
            model.SearchFriendlyUrlName,
            model.SearchFirstName,
            model.SearchLastName,
            model.LoadOnlyWithOrders,
            model.OrdersCreatedFromUtc,
            model.OrdersCreatedToUtc,
            command.Page - 1,
            command.PageSize,
            showHidden: true);

        var gridData = new List<AffiliateGridModel>();
        foreach (var a in affiliates)
        {
            var addr = await addressService.GetAddressByIdAsync(a.AddressId);
            gridData.Add(new AffiliateGridModel
            {
                Id = a.Id,
                Name = addr is not null ? a.GetFullName(addr) : string.Empty,
                Active = a.Active
            });
        }

        return Json(new DataSourceResult { Data = gridData, Total = affiliates.TotalCount });
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageAffiliates"))
            return Forbid();

        var model = new AffiliateModel { Active = true };
        await PrepareAddressDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AffiliateModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageAffiliates"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var address = MapToAddress(model);
            address.CreatedOnUtc = DateTime.UtcNow;
            await addressService.InsertAddressAsync(address);

            var affiliate = new Affiliate
            {
                Active = model.Active,
                AdminComment = model.AdminComment,
                AddressId = address.Id
            };
            affiliate.FriendlyUrlName = await affiliate.ValidateFriendlyUrlNameAsync(affiliateService, model.FriendlyUrlName);
            await affiliateService.InsertAffiliateAsync(affiliate);

            customerActivityService.InsertActivity("AddNewAffiliate", $"Added a new affiliate (ID = {affiliate.Id})");

            return continueEditing ? RedirectToAction("Edit", new { id = affiliate.Id }) : RedirectToAction("List");
        }

        await PrepareAddressDropdownsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageAffiliates"))
            return Forbid();

        var affiliate = await affiliateService.GetAffiliateByIdAsync(id);
        if (affiliate is null || affiliate.Deleted)
            return RedirectToAction("List");

        var address = await addressService.GetAddressByIdAsync(affiliate.AddressId);
        var model = MapToModel(affiliate, address);
        model.Url = affiliate.GenerateUrl(webHelper);
        await PrepareAddressDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(AffiliateModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageAffiliates"))
            return Forbid();

        var affiliate = await affiliateService.GetAffiliateByIdAsync(model.Id);
        if (affiliate is null || affiliate.Deleted)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            affiliate.Active = model.Active;
            affiliate.AdminComment = model.AdminComment;
            affiliate.FriendlyUrlName = await affiliate.ValidateFriendlyUrlNameAsync(affiliateService, model.FriendlyUrlName);

            var address = await addressService.GetAddressByIdAsync(affiliate.AddressId);
            if (address is not null)
            {
                MapToAddress(model, address);
                await addressService.UpdateAddressAsync(address);
            }

            await affiliateService.UpdateAffiliateAsync(affiliate);

            customerActivityService.InsertActivity("EditAffiliate", $"Edited an affiliate (ID = {affiliate.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = affiliate.Id });

            return RedirectToAction("List");
        }

        await PrepareAddressDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageAffiliates"))
            return Forbid();

        var affiliate = await affiliateService.GetAffiliateByIdAsync(id);
        if (affiliate is null)
            return RedirectToAction("List");

        await affiliateService.DeleteAffiliateAsync(affiliate);

        customerActivityService.InsertActivity("DeleteAffiliate", $"Deleted an affiliate (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Affiliated orders

    [HttpPost]
    public async Task<JsonResult> AffiliatedOrderList(int affiliateId, DataSourceRequest command,
        DateTime? startDate = null, DateTime? endDate = null,
        int orderStatusId = 0, int paymentStatusId = 0, int shippingStatusId = 0)
    {
        if (!permissionService.Authorize("ManageAffiliates"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var startDateUtc = startDate.HasValue
            ? dateTimeHelper.ConvertToUtcTime(startDate.Value, dateTimeHelper.CurrentTimeZone)
            : (DateTime?)null;
        var endDateUtc = endDate.HasValue
            ? dateTimeHelper.ConvertToUtcTime(endDate.Value, dateTimeHelper.CurrentTimeZone).AddDays(1)
            : (DateTime?)null;

        var osIds = orderStatusId > 0 ? new List<int> { orderStatusId } : null;
        var psIds = paymentStatusId > 0 ? new List<int> { paymentStatusId } : null;
        var ssIds = shippingStatusId > 0 ? new List<int> { shippingStatusId } : null;

        var orders = await orderService.SearchOrdersAsync(
            createdFromUtc: startDateUtc,
            createdToUtc: endDateUtc,
            osIds: osIds,
            psIds: psIds,
            ssIds: ssIds,
            affiliateId: affiliateId,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var gridData = new List<AffiliatedOrderModel>();
        foreach (var o in orders)
        {
            gridData.Add(new AffiliatedOrderModel
            {
                Id = o.Id,
                CustomOrderNumber = o.CustomOrderNumber,
                OrderStatus = ((Nop.Core.Domain.Orders.OrderStatus)o.OrderStatusId).ToString(),
                PaymentStatus = ((Nop.Core.Domain.Payments.PaymentStatus)o.PaymentStatusId).ToString(),
                ShippingStatus = ((Nop.Core.Domain.Shipping.ShippingStatus)o.ShippingStatusId).ToString(),
                OrderTotal = await priceFormatter.FormatPriceAsync(o.OrderTotal, true, false),
                CreatedOn = dateTimeHelper.ConvertToUserTime(o.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        return Json(new DataSourceResult { Data = gridData, Total = orders.TotalCount });
    }

    #endregion

    #region Affiliated customers

    [HttpPost]
    public async Task<JsonResult> AffiliatedCustomerList(int affiliateId, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageAffiliates"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var customers = await customerService.GetAllCustomersAsync(
            affiliateId: affiliateId,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var gridData = customers.Select(c => new AffiliatedCustomerModel
        {
            Id = c.Id,
            Name = c.Email
        });

        return Json(new DataSourceResult { Data = gridData, Total = customers.TotalCount });
    }

    #endregion

    #region Helpers

    private async Task PrepareAddressDropdownsAsync(AffiliateModel model)
    {
        model.AvailableCountries.Insert(0, new SelectListItem { Text = "Select country", Value = "0" });
        foreach (var c in await countryService.GetAllCountriesAsync(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

        if (model.CountryId.HasValue && model.CountryId.Value > 0)
        {
            var states = await stateProvinceService.GetStateProvincesByCountryIdAsync(model.CountryId.Value, showHidden: true);
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
        }

        if (model.AvailableStates.Count == 0)
            model.AvailableStates.Add(new SelectListItem { Text = "Other (Non US)", Value = "0" });
    }

    private static AffiliateModel MapToModel(Affiliate affiliate, Address? address)
    {
        var model = new AffiliateModel
        {
            Id = affiliate.Id,
            Active = affiliate.Active,
            AdminComment = affiliate.AdminComment,
            FriendlyUrlName = affiliate.FriendlyUrlName
        };

        if (address is not null)
        {
            model.FirstName = address.FirstName;
            model.LastName = address.LastName;
            model.Email = address.Email;
            model.Company = address.Company;
            model.CountryId = address.CountryId;
            model.StateProvinceId = address.StateProvinceId;
            model.City = address.City;
            model.Address1 = address.Address1;
            model.Address2 = address.Address2;
            model.ZipPostalCode = address.ZipPostalCode;
            model.PhoneNumber = address.PhoneNumber;
            model.FaxNumber = address.FaxNumber;
        }

        return model;
    }

    private static Address MapToAddress(AffiliateModel model, Address? existing = null)
    {
        var address = existing ?? new Address();
        address.FirstName = model.FirstName;
        address.LastName = model.LastName;
        address.Email = model.Email;
        address.Company = model.Company;
        address.CountryId = model.CountryId is > 0 ? model.CountryId : null;
        address.StateProvinceId = model.StateProvinceId is > 0 ? model.StateProvinceId : null;
        address.City = model.City;
        address.Address1 = model.Address1;
        address.Address2 = model.Address2;
        address.ZipPostalCode = model.ZipPostalCode;
        address.PhoneNumber = model.PhoneNumber;
        address.FaxNumber = model.FaxNumber;
        return address;
    }

    #endregion
}
