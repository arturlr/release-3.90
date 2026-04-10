using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.ExportImport;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Models.Customers;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class CustomerController(
    ICustomerService customerService,
    ICustomerRegistrationService customerRegistrationService,
    IGenericAttributeService genericAttributeService,
    IDateTimeHelper dateTimeHelper,
    ICountryService countryService,
    IStateProvinceService stateProvinceService,
    IVendorService vendorService,
    IStoreService storeService,
    IExportManager exportManager,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    IStoreContext storeContext,
    CustomerSettings customerSettings) : BaseAdminController
{
    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        var defaultRoleIds = new List<int>();
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        if (registeredRole is not null)
            defaultRoleIds.Add(registeredRole.Id);

        var model = new CustomerListModel
        {
            UsernamesEnabled = customerSettings.UsernamesEnabled,
            DateOfBirthEnabled = customerSettings.DateOfBirthEnabled,
            CompanyEnabled = customerSettings.CompanyEnabled,
            PhoneEnabled = customerSettings.PhoneEnabled,
            ZipPostalCodeEnabled = customerSettings.ZipPostalCodeEnabled,
            SearchCustomerRoleIds = defaultRoleIds
        };

        foreach (var role in await customerService.GetAllCustomerRolesAsync(true))
            model.AvailableCustomerRoles.Add(new SelectListItem
            {
                Text = role.Name,
                Value = role.Id.ToString(),
                Selected = defaultRoleIds.Contains(role.Id)
            });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> CustomerList(DataSourceRequest command, CustomerListModel model)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        int searchDayOfBirth = 0, searchMonthOfBirth = 0;
        if (!string.IsNullOrWhiteSpace(model.SearchDayOfBirth))
            int.TryParse(model.SearchDayOfBirth, out searchDayOfBirth);
        if (!string.IsNullOrWhiteSpace(model.SearchMonthOfBirth))
            int.TryParse(model.SearchMonthOfBirth, out searchMonthOfBirth);

        var customers = await customerService.GetAllCustomersAsync(
            customerRoleIds: model.SearchCustomerRoleIds.ToArray(),
            email: model.SearchEmail,
            username: model.SearchUsername,
            firstName: model.SearchFirstName,
            lastName: model.SearchLastName,
            dayOfBirth: searchDayOfBirth,
            monthOfBirth: searchMonthOfBirth,
            company: model.SearchCompany,
            phone: model.SearchPhone,
            zipPostalCode: model.SearchZipPostalCode,
            ipAddress: model.SearchIpAddress,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var gridModels = new List<CustomerGridModel>();
        foreach (var c in customers)
            gridModels.Add(await PrepareCustomerGridModelAsync(c));

        return Json(new DataSourceResult { Data = gridModels, Total = customers.TotalCount });
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        var model = new CustomerModel { Active = true };
        await PrepareCustomerModelDropdownsAsync(model, null);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var existing = await customerService.GetCustomerByEmailAsync(model.Email);
            if (existing is not null)
                ModelState.AddModelError("", "Email is already registered");
        }

        if (customerSettings.UsernamesEnabled && !string.IsNullOrWhiteSpace(model.Username))
        {
            var existing = await customerService.GetCustomerByUsernameAsync(model.Username);
            if (existing is not null)
                ModelState.AddModelError("", "Username is already registered");
        }

        var allRoles = await customerService.GetAllCustomerRolesAsync(true);
        var newRoles = allRoles.Where(r => model.SelectedCustomerRoleIds.Contains(r.Id)).ToList();
        var rolesError = ValidateCustomerRoles(newRoles);
        if (!string.IsNullOrEmpty(rolesError))
            ModelState.AddModelError("", rolesError);

        if (newRoles.Any(r => r.SystemName == SystemCustomerRoleNames.Registered)
            && !CommonHelper.IsValidEmail(model.Email))
            ModelState.AddModelError("", "Valid email is required for the 'Registered' role");

        if (!ModelState.IsValid)
        {
            await PrepareCustomerModelDropdownsAsync(model, null);
            return View(model);
        }

        var customer = new Customer
        {
            CustomerGuid = Guid.NewGuid(),
            Email = model.Email,
            Username = model.Username,
            VendorId = model.VendorId,
            AdminComment = model.AdminComment,
            IsTaxExempt = model.IsTaxExempt,
            Active = model.Active,
            CreatedOnUtc = DateTime.UtcNow,
            LastActivityDateUtc = DateTime.UtcNow,
            RegisteredInStoreId = storeContext.CurrentStore.Id
        };
        await customerService.InsertCustomerAsync(customer);

        await SaveCustomerFormFieldsAsync(customer, model);

        // Password
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var changePassRequest = new ChangePasswordRequest(
                model.Email ?? string.Empty, false,
                customerSettings.DefaultPasswordFormat, model.Password);
            var changePassResult = await customerRegistrationService.ChangePasswordAsync(changePassRequest);
            if (!changePassResult.Success)
                foreach (var error in changePassResult.Errors)
                    ErrorNotification(error);
        }

        // Customer roles
        foreach (var role in newRoles)
            await customerService.AddCustomerRoleMappingAsync(
                new CustomerCustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = role.Id });

        // Vendor-admin guard
        var adminRole = newRoles.FirstOrDefault(r => r.SystemName == SystemCustomerRoleNames.Administrators);
        if (adminRole is not null && customer.VendorId > 0)
        {
            customer.VendorId = 0;
            await customerService.UpdateCustomerAsync(customer);
        }

        customerActivityService.InsertActivity("AddNewCustomer", $"Added a new customer (ID = {customer.Id})");

        if (continueEditing)
            return RedirectToAction("Edit", new { id = customer.Id });

        return RedirectToAction("List");
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        var customer = await customerService.GetCustomerByIdAsync(id);
        if (customer is null || customer.Deleted)
            return RedirectToAction("List");

        var model = await MapEntityToModelAsync(customer);
        await PrepareCustomerModelDropdownsAsync(model, customer);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CustomerModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        var customer = await customerService.GetCustomerByIdAsync(model.Id);
        if (customer is null || customer.Deleted)
            return RedirectToAction("List");

        var allRoles = await customerService.GetAllCustomerRolesAsync(true);
        var newRoles = allRoles.Where(r => model.SelectedCustomerRoleIds.Contains(r.Id)).ToList();
        var rolesError = ValidateCustomerRoles(newRoles);
        if (!string.IsNullOrEmpty(rolesError))
            ModelState.AddModelError("", rolesError);

        if (newRoles.Any(r => r.SystemName == SystemCustomerRoleNames.Registered)
            && !CommonHelper.IsValidEmail(model.Email))
            ModelState.AddModelError("", "Valid email is required for the 'Registered' role");

        if (!ModelState.IsValid)
        {
            await PrepareCustomerModelDropdownsAsync(model, customer);
            return View(model);
        }

        customer.AdminComment = model.AdminComment;
        customer.IsTaxExempt = model.IsTaxExempt;
        customer.VendorId = model.VendorId;

        // Prevent deactivation of last admin
        var adminRoleDef = allRoles.FirstOrDefault(r => r.SystemName == SystemCustomerRoleNames.Administrators);
        var currentRoleIds = await customerService.GetCustomerRoleIdsAsync(customer, true);
        bool isAdmin = adminRoleDef is not null && currentRoleIds.Contains(adminRoleDef.Id);
        if (!isAdmin || model.Active || await SecondAdminAccountExistsAsync(customer))
            customer.Active = model.Active;

        // Email
        if (!string.IsNullOrWhiteSpace(model.Email))
            await customerRegistrationService.SetEmailAsync(customer, model.Email, false);
        else
            customer.Email = model.Email;

        // Username
        if (customerSettings.UsernamesEnabled && !string.IsNullOrWhiteSpace(model.Username))
            await customerRegistrationService.SetUsernameAsync(customer, model.Username);

        await customerService.UpdateCustomerAsync(customer);

        await SaveCustomerFormFieldsAsync(customer, model);

        // Sync roles
        var existingRoleIds = await customerService.GetCustomerRoleIdsAsync(customer, true);
        foreach (var role in allRoles)
        {
            bool shouldHave = model.SelectedCustomerRoleIds.Contains(role.Id);
            bool has = existingRoleIds.Contains(role.Id);
            if (shouldHave && !has)
                await customerService.AddCustomerRoleMappingAsync(
                    new CustomerCustomerRoleMapping { CustomerId = customer.Id, CustomerRoleId = role.Id });
            else if (!shouldHave && has)
                await customerService.RemoveCustomerRoleMappingAsync(customer, role);
        }

        // Vendor-admin guard
        var newRoleIds = await customerService.GetCustomerRoleIdsAsync(customer, true);
        if (adminRoleDef is not null && newRoleIds.Contains(adminRoleDef.Id) && customer.VendorId > 0)
        {
            customer.VendorId = 0;
            await customerService.UpdateCustomerAsync(customer);
        }

        customerActivityService.InsertActivity("EditCustomer", $"Edited a customer (ID = {customer.Id})");

        if (continueEditing)
            return RedirectToAction("Edit", new { id = customer.Id });

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        var customer = await customerService.GetCustomerByIdAsync(id);
        if (customer is null)
            return RedirectToAction("List");

        // Prevent deleting last admin
        var adminRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Administrators);
        if (adminRole is not null)
        {
            var roleIds = await customerService.GetCustomerRoleIdsAsync(customer, true);
            if (roleIds.Contains(adminRole.Id) && !await SecondAdminAccountExistsAsync(customer))
                return RedirectToAction("Edit", new { id = customer.Id });
        }

        await customerService.DeleteCustomerAsync(customer);

        customerActivityService.InsertActivity("DeleteCustomer", $"Deleted a customer (ID = {id})");

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        if (selectedIds is not null)
        {
            var customers = await customerService.GetCustomersByIdsAsync(selectedIds.ToArray());
            foreach (var customer in customers)
                await customerService.DeleteCustomerAsync(customer);
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> ExportExcelAll(CustomerListModel model)
    {
        if (!permissionService.Authorize("ManageCustomers"))
            return Forbid();

        int searchDayOfBirth = 0, searchMonthOfBirth = 0;
        if (!string.IsNullOrWhiteSpace(model.SearchDayOfBirth))
            int.TryParse(model.SearchDayOfBirth, out searchDayOfBirth);
        if (!string.IsNullOrWhiteSpace(model.SearchMonthOfBirth))
            int.TryParse(model.SearchMonthOfBirth, out searchMonthOfBirth);

        var customers = await customerService.GetAllCustomersAsync(
            customerRoleIds: model.SearchCustomerRoleIds.ToArray(),
            email: model.SearchEmail,
            username: model.SearchUsername,
            firstName: model.SearchFirstName,
            lastName: model.SearchLastName,
            dayOfBirth: searchDayOfBirth,
            monthOfBirth: searchMonthOfBirth,
            company: model.SearchCompany,
            phone: model.SearchPhone,
            zipPostalCode: model.SearchZipPostalCode,
            ipAddress: model.SearchIpAddress);

        var bytes = await exportManager.ExportCustomersToXlsxAsync(customers.ToList());
        return File(bytes, MimeTypes.TextXlsx, "customers.xlsx");
    }
}
