using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Web.Areas.Admin.Models.Shipping;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ShippingController(
    IShippingService shippingService,
    IDateRangeService dateRangeService,
    IAddressService addressService,
    ICountryService countryService,
    IStateProvinceService stateProvinceService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Shipping methods

    public IActionResult Methods()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();
        return View();
    }

    [HttpPost]
    public async Task<JsonResult> MethodList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var methods = await shippingService.GetAllShippingMethodsAsync();
        var gridModel = new DataSourceResult
        {
            Data = methods.Select(m => new ShippingMethodModel
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                DisplayOrder = m.DisplayOrder
            }),
            Total = methods.Count
        };
        return Json(gridModel);
    }

    public IActionResult CreateMethod()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();
        return View(new ShippingMethodModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateMethod(ShippingMethodModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var sm = new ShippingMethod
            {
                Name = model.Name,
                Description = model.Description,
                DisplayOrder = model.DisplayOrder
            };
            await shippingService.InsertShippingMethodAsync(sm);
            customerActivityService.InsertActivity("AddNewShippingMethod", "Added a new shipping method ('{0}')", sm.Name ?? string.Empty);
            SuccessNotification("The new shipping method has been added.");
            return continueEditing ? RedirectToAction("EditMethod", new { id = sm.Id }) : RedirectToAction("Methods");
        }
        return View(model);
    }

    public async Task<IActionResult> EditMethod(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var sm = await shippingService.GetShippingMethodByIdAsync(id);
        if (sm == null)
            return RedirectToAction("Methods");

        var model = new ShippingMethodModel
        {
            Id = sm.Id,
            Name = sm.Name,
            Description = sm.Description,
            DisplayOrder = sm.DisplayOrder
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditMethod(ShippingMethodModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var sm = await shippingService.GetShippingMethodByIdAsync(model.Id);
        if (sm == null)
            return RedirectToAction("Methods");

        if (ModelState.IsValid)
        {
            sm.Name = model.Name;
            sm.Description = model.Description;
            sm.DisplayOrder = model.DisplayOrder;
            await shippingService.UpdateShippingMethodAsync(sm);
            customerActivityService.InsertActivity("EditShippingMethod", "Edited a shipping method ('{0}')", sm.Name ?? string.Empty);
            SuccessNotification("The shipping method has been updated.");
            return continueEditing ? RedirectToAction("EditMethod", new { id = sm.Id }) : RedirectToAction("Methods");
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMethod(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var sm = await shippingService.GetShippingMethodByIdAsync(id);
        if (sm == null)
            return RedirectToAction("Methods");

        await shippingService.DeleteShippingMethodAsync(sm);
        customerActivityService.InsertActivity("DeleteShippingMethod", "Deleted a shipping method ('{0}')", sm.Name ?? string.Empty);
        SuccessNotification("The shipping method has been deleted.");
        return RedirectToAction("Methods");
    }

    #endregion

    #region Dates and ranges

    public IActionResult DatesAndRanges()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();
        return View();
    }

    // Delivery dates

    [HttpPost]
    public async Task<JsonResult> DeliveryDateList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var dates = await dateRangeService.GetAllDeliveryDatesAsync();
        var gridModel = new DataSourceResult
        {
            Data = dates.Select(d => new DeliveryDateModel { Id = d.Id, Name = d.Name, DisplayOrder = d.DisplayOrder }),
            Total = dates.Count
        };
        return Json(gridModel);
    }

    public IActionResult CreateDeliveryDate()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();
        return View(new DeliveryDateModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeliveryDate(DeliveryDateModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var dd = new DeliveryDate { Name = model.Name, DisplayOrder = model.DisplayOrder };
            await dateRangeService.InsertDeliveryDateAsync(dd);
            SuccessNotification("The new delivery date has been added.");
            return continueEditing ? RedirectToAction("EditDeliveryDate", new { id = dd.Id }) : RedirectToAction("DatesAndRanges");
        }
        return View(model);
    }

    public async Task<IActionResult> EditDeliveryDate(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var dd = await dateRangeService.GetDeliveryDateByIdAsync(id);
        if (dd == null)
            return RedirectToAction("DatesAndRanges");

        return View(new DeliveryDateModel { Id = dd.Id, Name = dd.Name, DisplayOrder = dd.DisplayOrder });
    }

    [HttpPost]
    public async Task<IActionResult> EditDeliveryDate(DeliveryDateModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var dd = await dateRangeService.GetDeliveryDateByIdAsync(model.Id);
        if (dd == null)
            return RedirectToAction("DatesAndRanges");

        if (ModelState.IsValid)
        {
            dd.Name = model.Name;
            dd.DisplayOrder = model.DisplayOrder;
            await dateRangeService.UpdateDeliveryDateAsync(dd);
            SuccessNotification("The delivery date has been updated.");
            return continueEditing ? RedirectToAction("EditDeliveryDate", new { id = dd.Id }) : RedirectToAction("DatesAndRanges");
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDeliveryDate(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var dd = await dateRangeService.GetDeliveryDateByIdAsync(id);
        if (dd == null)
            return RedirectToAction("DatesAndRanges");

        await dateRangeService.DeleteDeliveryDateAsync(dd);
        SuccessNotification("The delivery date has been deleted.");
        return RedirectToAction("DatesAndRanges");
    }

    // Product availability ranges

    [HttpPost]
    public async Task<JsonResult> ProductAvailabilityRangeList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var ranges = await dateRangeService.GetAllProductAvailabilityRangesAsync();
        var gridModel = new DataSourceResult
        {
            Data = ranges.Select(r => new ProductAvailabilityRangeModel { Id = r.Id, Name = r.Name, DisplayOrder = r.DisplayOrder }),
            Total = ranges.Count
        };
        return Json(gridModel);
    }

    public IActionResult CreateProductAvailabilityRange()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();
        return View(new ProductAvailabilityRangeModel());
    }

    [HttpPost]
    public async Task<IActionResult> CreateProductAvailabilityRange(ProductAvailabilityRangeModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var par = new ProductAvailabilityRange { Name = model.Name, DisplayOrder = model.DisplayOrder };
            await dateRangeService.InsertProductAvailabilityRangeAsync(par);
            SuccessNotification("The new product availability range has been added.");
            return continueEditing ? RedirectToAction("EditProductAvailabilityRange", new { id = par.Id }) : RedirectToAction("DatesAndRanges");
        }
        return View(model);
    }

    public async Task<IActionResult> EditProductAvailabilityRange(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var par = await dateRangeService.GetProductAvailabilityRangeByIdAsync(id);
        if (par == null)
            return RedirectToAction("DatesAndRanges");

        return View(new ProductAvailabilityRangeModel { Id = par.Id, Name = par.Name, DisplayOrder = par.DisplayOrder });
    }

    [HttpPost]
    public async Task<IActionResult> EditProductAvailabilityRange(ProductAvailabilityRangeModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var par = await dateRangeService.GetProductAvailabilityRangeByIdAsync(model.Id);
        if (par == null)
            return RedirectToAction("DatesAndRanges");

        if (ModelState.IsValid)
        {
            par.Name = model.Name;
            par.DisplayOrder = model.DisplayOrder;
            await dateRangeService.UpdateProductAvailabilityRangeAsync(par);
            SuccessNotification("The product availability range has been updated.");
            return continueEditing ? RedirectToAction("EditProductAvailabilityRange", new { id = par.Id }) : RedirectToAction("DatesAndRanges");
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProductAvailabilityRange(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var par = await dateRangeService.GetProductAvailabilityRangeByIdAsync(id);
        if (par == null)
            return RedirectToAction("DatesAndRanges");

        await dateRangeService.DeleteProductAvailabilityRangeAsync(par);
        SuccessNotification("The product availability range has been deleted.");
        return RedirectToAction("DatesAndRanges");
    }

    #endregion

    #region Warehouses

    public IActionResult Warehouses()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();
        return View();
    }

    [HttpPost]
    public async Task<JsonResult> WarehouseList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var warehouses = await shippingService.GetAllWarehousesAsync();
        var gridModel = new DataSourceResult
        {
            Data = warehouses.Select(w => new { w.Id, w.Name }),
            Total = warehouses.Count
        };
        return Json(gridModel);
    }

    public async Task<IActionResult> CreateWarehouse()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var model = new WarehouseModel();
        await PrepareWarehouseAddressModelAsync(model.Address, null);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWarehouse(WarehouseModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var address = new Nop.Core.Domain.Common.Address
            {
                CountryId = model.Address.CountryId,
                StateProvinceId = model.Address.StateProvinceId,
                City = model.Address.City,
                Address1 = model.Address.Address1,
                ZipPostalCode = model.Address.ZipPostalCode,
                PhoneNumber = model.Address.PhoneNumber,
                CreatedOnUtc = DateTime.UtcNow
            };
            await addressService.InsertAddressAsync(address);

            var warehouse = new Warehouse
            {
                Name = model.Name,
                AdminComment = model.AdminComment,
                AddressId = address.Id
            };
            await shippingService.InsertWarehouseAsync(warehouse);
            customerActivityService.InsertActivity("AddNewWarehouse", "Added a new warehouse ('{0}')", warehouse.Name ?? string.Empty);
            SuccessNotification("The new warehouse has been added.");
            return continueEditing ? RedirectToAction("EditWarehouse", new { id = warehouse.Id }) : RedirectToAction("Warehouses");
        }

        await PrepareWarehouseAddressModelAsync(model.Address, null);
        return View(model);
    }

    public async Task<IActionResult> EditWarehouse(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var warehouse = await shippingService.GetWarehouseByIdAsync(id);
        if (warehouse == null)
            return RedirectToAction("Warehouses");

        var address = await addressService.GetAddressByIdAsync(warehouse.AddressId);
        var model = new WarehouseModel
        {
            Id = warehouse.Id,
            Name = warehouse.Name,
            AdminComment = warehouse.AdminComment
        };
        await PrepareWarehouseAddressModelAsync(model.Address, address);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditWarehouse(WarehouseModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var warehouse = await shippingService.GetWarehouseByIdAsync(model.Id);
        if (warehouse == null)
            return RedirectToAction("Warehouses");

        if (ModelState.IsValid)
        {
            var address = await addressService.GetAddressByIdAsync(warehouse.AddressId)
                ?? new Nop.Core.Domain.Common.Address { CreatedOnUtc = DateTime.UtcNow };

            address.CountryId = model.Address.CountryId;
            address.StateProvinceId = model.Address.StateProvinceId;
            address.City = model.Address.City;
            address.Address1 = model.Address.Address1;
            address.ZipPostalCode = model.Address.ZipPostalCode;
            address.PhoneNumber = model.Address.PhoneNumber;

            if (address.Id > 0)
                await addressService.UpdateAddressAsync(address);
            else
                await addressService.InsertAddressAsync(address);

            warehouse.Name = model.Name;
            warehouse.AdminComment = model.AdminComment;
            warehouse.AddressId = address.Id;
            await shippingService.UpdateWarehouseAsync(warehouse);
            customerActivityService.InsertActivity("EditWarehouse", "Edited a warehouse ('{0}')", warehouse.Name ?? string.Empty);
            SuccessNotification("The warehouse has been updated.");
            return continueEditing ? RedirectToAction("EditWarehouse", new { id = warehouse.Id }) : RedirectToAction("Warehouses");
        }

        var existingAddress = await addressService.GetAddressByIdAsync(warehouse.AddressId);
        await PrepareWarehouseAddressModelAsync(model.Address, existingAddress);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var warehouse = await shippingService.GetWarehouseByIdAsync(id);
        if (warehouse == null)
            return RedirectToAction("Warehouses");

        await shippingService.DeleteWarehouseAsync(warehouse);
        customerActivityService.InsertActivity("DeleteWarehouse", "Deleted a warehouse ('{0}')", warehouse.Name ?? string.Empty);
        SuccessNotification("The warehouse has been deleted.");
        return RedirectToAction("Warehouses");
    }

    #endregion

    #region Restrictions

    public async Task<IActionResult> Restrictions()
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var model = new ShippingMethodRestrictionModel();
        var countries = await countryService.GetAllCountriesAsync(showHidden: true);
        var methods = await shippingService.GetAllShippingMethodsAsync();
        var allMappings = await shippingService.GetAllShippingMethodCountryMappingsAsync();

        foreach (var c in countries)
            model.AvailableCountries.Add(new CountryModel { Id = c.Id, Name = c.Name });
        foreach (var sm in methods)
            model.AvailableShippingMethods.Add(new ShippingMethodModel { Id = sm.Id, Name = sm.Name });

        var mappingSet = allMappings.Select(m => (m.ShippingMethodId, m.CountryId)).ToHashSet();
        foreach (var c in countries)
        {
            model.Restricted[c.Id] = [];
            foreach (var sm in methods)
                model.Restricted[c.Id][sm.Id] = mappingSet.Contains((sm.Id, c.Id));
        }

        return View(model);
    }

    [HttpPost, ActionName("Restrictions")]
    public async Task<IActionResult> RestrictionSave(IFormCollection form)
    {
        if (!permissionService.Authorize("ManageShippingSettings"))
            return Forbid();

        var countries = await countryService.GetAllCountriesAsync(showHidden: true);
        var methods = await shippingService.GetAllShippingMethodsAsync();
        var allMappings = await shippingService.GetAllShippingMethodCountryMappingsAsync();

        foreach (var sm in methods)
        {
            var formKey = "restrict_" + sm.Id;
            var restrictedCountryIds = form.ContainsKey(formKey)
                ? form[formKey].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet()
                : new HashSet<int>();

            foreach (var c in countries)
            {
                var existing = allMappings.FirstOrDefault(m => m.ShippingMethodId == sm.Id && m.CountryId == c.Id);
                if (restrictedCountryIds.Contains(c.Id))
                {
                    if (existing == null)
                        await shippingService.InsertShippingMethodCountryMappingAsync(
                            new ShippingMethodCountryMapping { ShippingMethodId = sm.Id, CountryId = c.Id });
                }
                else
                {
                    if (existing != null)
                        await shippingService.DeleteShippingMethodCountryMappingAsync(existing);
                }
            }
        }

        SuccessNotification("Shipping restrictions have been updated.");
        return RedirectToAction("Restrictions");
    }

    #endregion
}
