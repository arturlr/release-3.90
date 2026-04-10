using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Vendors;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Models.Vendors;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class VendorController(
    IVendorService vendorService,
    ICustomerService customerService,
    IAddressService addressService,
    ICountryService countryService,
    IStateProvinceService stateProvinceService,
    IUrlRecordService urlRecordService,
    IDateTimeHelper dateTimeHelper,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    VendorSettings vendorSettings,
    SeoSettings seoSettings) : BaseAdminController
{
    #region Vendors

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Forbid();

        return View(new VendorListModel());
    }

    [HttpPost]
    public async Task<JsonResult> VendorList(DataSourceRequest command, VendorListModel model)
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var vendors = await vendorService.GetAllVendorsAsync(
            model.SearchName ?? string.Empty,
            command.Page - 1,
            command.PageSize,
            showHidden: true);

        var gridData = vendors.Select(v => new VendorGridModel
        {
            Id = v.Id,
            Name = v.Name,
            Active = v.Active
        });

        return Json(new DataSourceResult { Data = gridData, Total = vendors.TotalCount });
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Forbid();

        var model = new VendorModel
        {
            Active = true,
            PageSize = 6,
            AllowCustomersToSelectPageSize = true,
            PageSizeOptions = vendorSettings.DefaultVendorPageSizeOptions
        };
        await PrepareAddressDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(VendorModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var vendor = new Vendor
            {
                Name = model.Name,
                Email = model.Email,
                Description = model.Description,
                AdminComment = model.AdminComment,
                PictureId = model.PictureId,
                Active = model.Active,
                DisplayOrder = model.DisplayOrder,
                MetaKeywords = model.MetaKeywords,
                MetaDescription = model.MetaDescription,
                MetaTitle = model.MetaTitle,
                PageSize = model.PageSize,
                AllowCustomersToSelectPageSize = model.AllowCustomersToSelectPageSize,
                PageSizeOptions = model.PageSizeOptions
            };
            await vendorService.InsertVendorAsync(vendor);

            // SEO slug
            var seName = await vendor.ValidateSeNameAsync(model.SeName, vendor.Name ?? string.Empty, true, urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(vendor, seName, 0);

            // Address
            var address = MapToAddress(model);
            address.CreatedOnUtc = DateTime.UtcNow;
            await addressService.InsertAddressAsync(address);
            vendor.AddressId = address.Id;
            await vendorService.UpdateVendorAsync(vendor);

            customerActivityService.InsertActivity("AddNewVendor", $"Added a new vendor (ID = {vendor.Id})");

            return continueEditing ? RedirectToAction("Edit", new { id = vendor.Id }) : RedirectToAction("List");
        }

        await PrepareAddressDropdownsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Forbid();

        var vendor = await vendorService.GetVendorByIdAsync(id);
        if (vendor is null || vendor.Deleted)
            return RedirectToAction("List");

        var address = await addressService.GetAddressByIdAsync(vendor.AddressId);
        var model = MapToModel(vendor, address);
        model.SeName = await urlRecordService.GetActiveSlugAsync(vendor.Id, "Vendor", 0);
        await PrepareAddressDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(VendorModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Forbid();

        var vendor = await vendorService.GetVendorByIdAsync(model.Id);
        if (vendor is null || vendor.Deleted)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            vendor.Name = model.Name;
            vendor.Email = model.Email;
            vendor.Description = model.Description;
            vendor.AdminComment = model.AdminComment;
            vendor.PictureId = model.PictureId;
            vendor.Active = model.Active;
            vendor.DisplayOrder = model.DisplayOrder;
            vendor.MetaKeywords = model.MetaKeywords;
            vendor.MetaDescription = model.MetaDescription;
            vendor.MetaTitle = model.MetaTitle;
            vendor.PageSize = model.PageSize;
            vendor.AllowCustomersToSelectPageSize = model.AllowCustomersToSelectPageSize;
            vendor.PageSizeOptions = model.PageSizeOptions;
            await vendorService.UpdateVendorAsync(vendor);

            // SEO slug
            var seName = await vendor.ValidateSeNameAsync(model.SeName, vendor.Name ?? string.Empty, true, urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(vendor, seName, 0);

            // Address
            var address = await addressService.GetAddressByIdAsync(vendor.AddressId);
            if (address is not null)
            {
                MapToAddress(model, address);
                await addressService.UpdateAddressAsync(address);
            }
            else
            {
                address = MapToAddress(model);
                address.CreatedOnUtc = DateTime.UtcNow;
                await addressService.InsertAddressAsync(address);
                vendor.AddressId = address.Id;
                await vendorService.UpdateVendorAsync(vendor);
            }

            customerActivityService.InsertActivity("EditVendor", $"Edited a vendor (ID = {vendor.Id})");

            return continueEditing ? RedirectToAction("Edit", new { id = vendor.Id }) : RedirectToAction("List");
        }

        await PrepareAddressDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Forbid();

        var vendor = await vendorService.GetVendorByIdAsync(id);
        if (vendor is null)
            return RedirectToAction("List");

        // Clear associated customer VendorId references
        var associatedCustomers = await customerService.GetAllCustomersAsync(vendorId: vendor.Id);
        foreach (var customer in associatedCustomers)
        {
            customer.VendorId = 0;
            await customerService.UpdateCustomerAsync(customer);
        }

        await vendorService.DeleteVendorAsync(vendor);

        customerActivityService.InsertActivity("DeleteVendor", $"Deleted a vendor (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Vendor notes

    [HttpPost]
    public async Task<JsonResult> VendorNoteList(int vendorId, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var notes = await vendorService.GetVendorNotesByVendorIdAsync(vendorId);

        var gridData = notes.Select(n => new VendorNoteModel
        {
            Id = n.Id,
            VendorId = n.VendorId,
            Note = WebUtility.HtmlEncode(n.Note ?? string.Empty).Replace("\n", "<br />"),
            CreatedOn = dateTimeHelper.ConvertToUserTime(n.CreatedOnUtc, DateTimeKind.Utc)
        });

        return Json(new DataSourceResult { Data = gridData, Total = notes.Count });
    }

    [HttpPost]
    public async Task<JsonResult> VendorNoteAdd(int vendorId, string? message)
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Json(new { Result = false });

        var vendor = await vendorService.GetVendorByIdAsync(vendorId);
        if (vendor is null)
            return Json(new { Result = false });

        await vendorService.InsertVendorNoteAsync(new VendorNote
        {
            VendorId = vendor.Id,
            Note = message,
            CreatedOnUtc = DateTime.UtcNow
        });

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<JsonResult> VendorNoteDelete(int id, int vendorId)
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var note = await vendorService.GetVendorNoteByIdAsync(id);
        if (note is not null)
            await vendorService.DeleteVendorNoteAsync(note);

        return Json(new { });
    }

    #endregion

    #region Associated customers

    [HttpPost]
    public async Task<JsonResult> AssociatedCustomerList(int vendorId, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageVendors"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var customers = await customerService.GetAllCustomersAsync(
            vendorId: vendorId,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var gridData = customers.Select(c => new AssociatedCustomerModel
        {
            Id = c.Id,
            Email = c.Email
        });

        return Json(new DataSourceResult { Data = gridData, Total = customers.TotalCount });
    }

    #endregion

    #region Helpers

    private async Task PrepareAddressDropdownsAsync(VendorModel model)
    {
        model.AvailableCountries.Insert(0, new SelectListItem { Text = "Select country", Value = "0" });
        foreach (var c in await countryService.GetAllCountriesAsync(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

        if (model.CountryId is > 0)
        {
            var states = await stateProvinceService.GetStateProvincesByCountryIdAsync(model.CountryId.Value, showHidden: true);
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
        }

        if (model.AvailableStates.Count == 0)
            model.AvailableStates.Add(new SelectListItem { Text = "Other (Non US)", Value = "0" });
    }

    private static VendorModel MapToModel(Vendor vendor, Address? address)
    {
        var model = new VendorModel
        {
            Id = vendor.Id,
            Name = vendor.Name,
            Email = vendor.Email,
            Description = vendor.Description,
            AdminComment = vendor.AdminComment,
            PictureId = vendor.PictureId,
            Active = vendor.Active,
            DisplayOrder = vendor.DisplayOrder,
            MetaKeywords = vendor.MetaKeywords,
            MetaDescription = vendor.MetaDescription,
            MetaTitle = vendor.MetaTitle,
            PageSize = vendor.PageSize,
            AllowCustomersToSelectPageSize = vendor.AllowCustomersToSelectPageSize,
            PageSizeOptions = vendor.PageSizeOptions
        };

        if (address is not null)
        {
            model.FirstName = address.FirstName;
            model.LastName = address.LastName;
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

    private static Address MapToAddress(VendorModel model, Address? existing = null)
    {
        var address = existing ?? new Address();
        address.FirstName = model.FirstName;
        address.LastName = model.LastName;
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
