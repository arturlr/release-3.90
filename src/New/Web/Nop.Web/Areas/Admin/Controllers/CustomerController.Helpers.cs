using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Customers;
using Nop.Services.Common;
using Nop.Web.Areas.Admin.Models.Customers;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class CustomerController
{
    private static string ValidateCustomerRoles(IList<CustomerRole> roles)
    {
        bool isGuest = roles.Any(r => r.SystemName == SystemCustomerRoleNames.Guests);
        bool isRegistered = roles.Any(r => r.SystemName == SystemCustomerRoleNames.Registered);

        if (isGuest && isRegistered)
            return "Customer cannot be in both 'Guests' and 'Registered' roles";
        if (!isGuest && !isRegistered)
            return "Customer must be in either 'Guests' or 'Registered' role";

        return string.Empty;
    }

    private async Task<bool> SecondAdminAccountExistsAsync(Customer customer)
    {
        var adminRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Administrators);
        if (adminRole is null) return false;

        var admins = await customerService.GetAllCustomersAsync(
            customerRoleIds: [adminRole.Id], pageSize: 2);
        return admins.Any(c => c.Active && c.Id != customer.Id);
    }

    private async Task<CustomerGridModel> PrepareCustomerGridModelAsync(Customer customer)
    {
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer, true);
        var allRoles = await customerService.GetAllCustomerRolesAsync(true);
        var roleNames = allRoles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name);

        var isRegistered = allRoles.Any(r => r.SystemName == SystemCustomerRoleNames.Registered && roleIds.Contains(r.Id));

        var firstName = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.FirstName, genericAttributeService);
        var lastName = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.LastName, genericAttributeService);

        return new CustomerGridModel
        {
            Id = customer.Id,
            Email = isRegistered ? customer.Email : "Guest",
            Username = customer.Username,
            FullName = $"{firstName} {lastName}".Trim(),
            Company = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.Company, genericAttributeService),
            Phone = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.Phone, genericAttributeService),
            ZipPostalCode = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.ZipPostalCode, genericAttributeService),
            CustomerRoleNames = string.Join(", ", roleNames),
            Active = customer.Active,
            CreatedOn = dateTimeHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc),
            LastActivityDate = dateTimeHelper.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc)
        };
    }

    private async Task<CustomerModel> MapEntityToModelAsync(Customer customer)
    {
        var stores = await storeService.GetAllStoresAsync();
        var registeredStore = stores.FirstOrDefault(s => s.Id == customer.RegisteredInStoreId);

        return new CustomerModel
        {
            Id = customer.Id,
            Email = customer.Email,
            Username = customer.Username,
            VendorId = customer.VendorId,
            AdminComment = customer.AdminComment,
            IsTaxExempt = customer.IsTaxExempt,
            Active = customer.Active,
            RegisteredInStore = registeredStore?.Name,
            CreatedOn = dateTimeHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc),
            LastActivityDate = dateTimeHelper.ConvertToUserTime(customer.LastActivityDateUtc, DateTimeKind.Utc),
            LastIpAddress = customer.LastIpAddress,
            TimeZoneId = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.TimeZoneId, genericAttributeService),
            LastVisitedPage = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.LastVisitedPage, genericAttributeService),
            FirstName = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.FirstName, genericAttributeService),
            LastName = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.LastName, genericAttributeService),
            Gender = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.Gender, genericAttributeService),
            DateOfBirth = await customer.GetAttributeAsync<DateTime?>(
                SystemCustomerAttributeNames.DateOfBirth, genericAttributeService),
            Company = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.Company, genericAttributeService),
            StreetAddress = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.StreetAddress, genericAttributeService),
            StreetAddress2 = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.StreetAddress2, genericAttributeService),
            ZipPostalCode = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.ZipPostalCode, genericAttributeService),
            City = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.City, genericAttributeService),
            CountryId = await customer.GetAttributeAsync<int>(
                SystemCustomerAttributeNames.CountryId, genericAttributeService),
            StateProvinceId = await customer.GetAttributeAsync<int>(
                SystemCustomerAttributeNames.StateProvinceId, genericAttributeService),
            Phone = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.Phone, genericAttributeService),
            Fax = await customer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.Fax, genericAttributeService),
            SelectedCustomerRoleIds = (await customerService.GetCustomerRoleIdsAsync(customer, true)).ToList()
        };
    }

    private async Task PrepareCustomerModelDropdownsAsync(CustomerModel model, Customer? customer)
    {
        model.UsernamesEnabled = customerSettings.UsernamesEnabled;
        model.GenderEnabled = customerSettings.GenderEnabled;
        model.DateOfBirthEnabled = customerSettings.DateOfBirthEnabled;
        model.CompanyEnabled = customerSettings.CompanyEnabled;
        model.StreetAddressEnabled = customerSettings.StreetAddressEnabled;
        model.StreetAddress2Enabled = customerSettings.StreetAddress2Enabled;
        model.ZipPostalCodeEnabled = customerSettings.ZipPostalCodeEnabled;
        model.CityEnabled = customerSettings.CityEnabled;
        model.CountryEnabled = customerSettings.CountryEnabled;
        model.StateProvinceEnabled = customerSettings.StateProvinceEnabled;
        model.PhoneEnabled = customerSettings.PhoneEnabled;
        model.FaxEnabled = customerSettings.FaxEnabled;

        // Vendors
        model.AvailableVendors.Add(new SelectListItem { Text = "None", Value = "0" });
        foreach (var v in await vendorService.GetAllVendorsAsync())
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

        // Customer roles
        var allRoles = await customerService.GetAllCustomerRolesAsync(true);
        if (customer is null)
        {
            // Pre-check Registered role for new customers
            var registeredRole = allRoles.FirstOrDefault(r => r.SystemName == SystemCustomerRoleNames.Registered);
            if (registeredRole is not null && !model.SelectedCustomerRoleIds.Contains(registeredRole.Id))
                model.SelectedCustomerRoleIds.Add(registeredRole.Id);
        }

        foreach (var role in allRoles)
            model.AvailableCustomerRoles.Add(new SelectListItem
            {
                Text = role.Name,
                Value = role.Id.ToString(),
                Selected = model.SelectedCustomerRoleIds.Contains(role.Id)
            });

        // Countries
        if (customerSettings.CountryEnabled)
        {
            model.AvailableCountries.Add(new SelectListItem { Text = "Select country", Value = "0" });
            foreach (var c in await countryService.GetAllCountriesAsync(showHidden: true))
                model.AvailableCountries.Add(new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                    Selected = c.Id == model.CountryId
                });

            // States
            if (customerSettings.StateProvinceEnabled)
            {
                var states = await stateProvinceService.GetStateProvincesByCountryIdAsync(model.CountryId);
                if (states.Any())
                {
                    model.AvailableStates.Add(new SelectListItem { Text = "Select state", Value = "0" });
                    foreach (var s in states)
                        model.AvailableStates.Add(new SelectListItem
                        {
                            Text = s.Name,
                            Value = s.Id.ToString(),
                            Selected = s.Id == model.StateProvinceId
                        });
                }
                else
                {
                    model.AvailableStates.Add(new SelectListItem { Text = "Other (Non US)", Value = "0" });
                }
            }
        }

        // Time zones
        foreach (var tz in dateTimeHelper.GetSystemTimeZones())
            model.AvailableTimeZones.Add(new SelectListItem
            {
                Text = tz.DisplayName,
                Value = tz.Id,
                Selected = tz.Id == model.TimeZoneId
            });
    }

    private async Task SaveCustomerFormFieldsAsync(Customer customer, CustomerModel model)
    {
        await genericAttributeService.SaveAttributeAsync(customer,
            SystemCustomerAttributeNames.FirstName, model.FirstName);
        await genericAttributeService.SaveAttributeAsync(customer,
            SystemCustomerAttributeNames.LastName, model.LastName);

        if (customerSettings.GenderEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.Gender, model.Gender);
        if (customerSettings.DateOfBirthEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.DateOfBirth, model.DateOfBirth);
        if (customerSettings.CompanyEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.Company, model.Company);
        if (customerSettings.StreetAddressEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.StreetAddress, model.StreetAddress);
        if (customerSettings.StreetAddress2Enabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.StreetAddress2, model.StreetAddress2);
        if (customerSettings.ZipPostalCodeEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.ZipPostalCode, model.ZipPostalCode);
        if (customerSettings.CityEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.City, model.City);
        if (customerSettings.CountryEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.CountryId, model.CountryId);
        if (customerSettings.CountryEnabled && customerSettings.StateProvinceEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.StateProvinceId, model.StateProvinceId);
        if (customerSettings.PhoneEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.Phone, model.Phone);
        if (customerSettings.FaxEnabled)
            await genericAttributeService.SaveAttributeAsync(customer,
                SystemCustomerAttributeNames.Fax, model.Fax);
    }
}
