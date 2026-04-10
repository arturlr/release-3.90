using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Customers;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public class CustomerRoleController(
    ICustomerService customerService,
    IProductService productService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<JsonResult> CustomerRoleList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var roles = await customerService.GetAllCustomerRolesAsync(true);

        var gridData = new List<CustomerRoleModel>();
        foreach (var role in roles)
        {
            var m = MapToModel(role);
            if (role.PurchasedWithProductId > 0)
            {
                var product = await productService.GetProductByIdAsync(role.PurchasedWithProductId);
                m.PurchasedWithProductName = product?.Name;
            }
            gridData.Add(m);
        }

        return Json(new DataSourceResult
        {
            Data = gridData,
            Total = roles.Count
        });
    }

    public IActionResult Create()
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        return View(new CustomerRoleModel { Active = true });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerRoleModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var customerRole = MapToEntity(model);
            await customerService.InsertCustomerRoleAsync(customerRole);

            customerActivityService.InsertActivity("AddNewCustomerRole",
                $"Added a new customer role ('{customerRole.Name}')", customerRole.Name ?? string.Empty);

            SuccessNotification("The new customer role has been added successfully.");
            return continueEditing
                ? RedirectToAction("Edit", new { id = customerRole.Id })
                : RedirectToAction("List");
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        var customerRole = await customerService.GetCustomerRoleByIdAsync(id);
        if (customerRole is null)
            return RedirectToAction("List");

        var model = MapToModel(customerRole);
        if (customerRole.PurchasedWithProductId > 0)
        {
            var product = await productService.GetProductByIdAsync(customerRole.PurchasedWithProductId);
            model.PurchasedWithProductName = product?.Name;
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CustomerRoleModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        var customerRole = await customerService.GetCustomerRoleByIdAsync(model.Id);
        if (customerRole is null)
            return RedirectToAction("List");

        try
        {
            if (ModelState.IsValid)
            {
                if (customerRole.IsSystemRole && !model.Active)
                    throw new NopException("You can't deactivate a system customer role.");

                if (customerRole.IsSystemRole &&
                    !string.Equals(customerRole.SystemName, model.SystemName, StringComparison.OrdinalIgnoreCase))
                    throw new NopException("You can't edit the system name of a system customer role.");

                if (string.Equals(customerRole.SystemName, SystemCustomerRoleNames.Registered, StringComparison.OrdinalIgnoreCase) &&
                    model.PurchasedWithProductId > 0)
                    throw new NopException("'Purchased with product' property can't be set for the 'Registered' customer role.");

                customerRole.Name = model.Name;
                customerRole.FreeShipping = model.FreeShipping;
                customerRole.TaxExempt = model.TaxExempt;
                customerRole.Active = model.Active;
                customerRole.EnablePasswordLifetime = model.EnablePasswordLifetime;
                customerRole.PurchasedWithProductId = model.PurchasedWithProductId;
                if (!customerRole.IsSystemRole)
                    customerRole.SystemName = model.SystemName;

                await customerService.UpdateCustomerRoleAsync(customerRole);

                customerActivityService.InsertActivity("EditCustomerRole",
                    $"Edited a customer role ('{customerRole.Name}')", customerRole.Name ?? string.Empty);

                SuccessNotification("The customer role has been updated successfully.");
                return continueEditing
                    ? RedirectToAction("Edit", new { id = customerRole.Id })
                    : RedirectToAction("List");
            }

            return View(model);
        }
        catch (Exception exc)
        {
            ErrorNotification(exc);
            return RedirectToAction("Edit", new { id = customerRole.Id });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        var customerRole = await customerService.GetCustomerRoleByIdAsync(id);
        if (customerRole is null)
            return RedirectToAction("List");

        try
        {
            await customerService.DeleteCustomerRoleAsync(customerRole);

            customerActivityService.InsertActivity("DeleteCustomerRole",
                $"Deleted a customer role ('{customerRole.Name}')", customerRole.Name ?? string.Empty);

            SuccessNotification("The customer role has been deleted successfully.");
            return RedirectToAction("List");
        }
        catch (Exception exc)
        {
            ErrorNotification(exc.Message);
            return RedirectToAction("Edit", new { id = customerRole.Id });
        }
    }

    #region Helpers

    private static CustomerRoleModel MapToModel(CustomerRole entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        FreeShipping = entity.FreeShipping,
        TaxExempt = entity.TaxExempt,
        Active = entity.Active,
        IsSystemRole = entity.IsSystemRole,
        SystemName = entity.SystemName,
        EnablePasswordLifetime = entity.EnablePasswordLifetime,
        PurchasedWithProductId = entity.PurchasedWithProductId
    };

    private static CustomerRole MapToEntity(CustomerRoleModel model) => new()
    {
        Name = model.Name,
        FreeShipping = model.FreeShipping,
        TaxExempt = model.TaxExempt,
        Active = model.Active,
        IsSystemRole = false,
        SystemName = model.SystemName,
        EnablePasswordLifetime = model.EnablePasswordLifetime,
        PurchasedWithProductId = model.PurchasedWithProductId
    };

    #endregion
}
