using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Web.Models.Customer;

namespace Nop.Web.Controllers;

public partial class CustomerController
{
    [HttpGet]
    public async Task<IActionResult> Addresses()
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var model = new CustomerAddressListModel();
        var mappings = customerAddressRepository.Table
            .Where(m => m.CustomerId == workContext.CurrentCustomer.Id).ToList();

        foreach (var mapping in mappings)
        {
            var address = await addressService.GetAddressByIdAsync(mapping.AddressId);
            if (address != null)
                model.Addresses.Add(MapAddress(address));
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddressDelete(int addressId)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var mapping = customerAddressRepository.Table
            .FirstOrDefault(m => m.CustomerId == workContext.CurrentCustomer.Id && m.AddressId == addressId);
        if (mapping != null)
        {
            customerAddressRepository.Delete(mapping);
            var address = await addressService.GetAddressByIdAsync(addressId);
            if (address != null)
                await addressService.DeleteAddressAsync(address);

            // Clear billing/shipping if this was the selected address
            var customer = workContext.CurrentCustomer;
            if (customer.BillingAddressId == addressId)
            {
                customer.BillingAddressId = null;
                await customerService.UpdateCustomerAsync(customer);
            }
            if (customer.ShippingAddressId == addressId)
            {
                customer.ShippingAddressId = null;
                await customerService.UpdateCustomerAsync(customer);
            }
        }
        return Json(new { redirect = Url.Action("Addresses") });
    }

    [HttpGet]
    public async Task<IActionResult> AddressAdd()
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();
        return View(new CustomerAddressEditModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddressAdd(CustomerAddressEditModel model)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        if (ModelState.IsValid)
        {
            var address = MapToAddress(model.Address);
            address.CreatedOnUtc = DateTime.UtcNow;
            await addressService.InsertAddressAsync(address);
            customerAddressRepository.Insert(new CustomerAddressMapping
            {
                CustomerId = workContext.CurrentCustomer.Id,
                AddressId = address.Id,
            });
            return RedirectToAction("Addresses");
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> AddressEdit(int addressId)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var mapping = customerAddressRepository.Table
            .FirstOrDefault(m => m.CustomerId == workContext.CurrentCustomer.Id && m.AddressId == addressId);
        if (mapping == null)
            return RedirectToAction("Addresses");

        var address = await addressService.GetAddressByIdAsync(addressId);
        if (address == null)
            return RedirectToAction("Addresses");

        return View(new CustomerAddressEditModel { Address = MapAddress(address) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddressEdit(CustomerAddressEditModel model, int addressId)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var mapping = customerAddressRepository.Table
            .FirstOrDefault(m => m.CustomerId == workContext.CurrentCustomer.Id && m.AddressId == addressId);
        if (mapping == null)
            return RedirectToAction("Addresses");

        if (ModelState.IsValid)
        {
            var address = await addressService.GetAddressByIdAsync(addressId);
            if (address == null)
                return RedirectToAction("Addresses");

            address.FirstName = model.Address.FirstName;
            address.LastName = model.Address.LastName;
            address.Email = model.Address.Email;
            address.Company = model.Address.Company;
            address.CountryId = model.Address.CountryId;
            address.StateProvinceId = model.Address.StateProvinceId;
            address.City = model.Address.City;
            address.Address1 = model.Address.Address1;
            address.Address2 = model.Address.Address2;
            address.ZipPostalCode = model.Address.ZipPostalCode;
            address.PhoneNumber = model.Address.PhoneNumber;
            address.FaxNumber = model.Address.FaxNumber;
            await addressService.UpdateAddressAsync(address);
            return RedirectToAction("Addresses");
        }
        return View(model);
    }

    private static AddressModel MapAddress(Address address) => new()
    {
        Id = address.Id,
        FirstName = address.FirstName,
        LastName = address.LastName,
        Email = address.Email,
        Company = address.Company,
        CountryId = address.CountryId,
        StateProvinceId = address.StateProvinceId,
        City = address.City,
        Address1 = address.Address1,
        Address2 = address.Address2,
        ZipPostalCode = address.ZipPostalCode,
        PhoneNumber = address.PhoneNumber,
        FaxNumber = address.FaxNumber,
    };

    private static Address MapToAddress(AddressModel model) => new()
    {
        FirstName = model.FirstName,
        LastName = model.LastName,
        Email = model.Email,
        Company = model.Company,
        CountryId = model.CountryId == 0 ? null : model.CountryId,
        StateProvinceId = model.StateProvinceId == 0 ? null : model.StateProvinceId,
        City = model.City,
        Address1 = model.Address1,
        Address2 = model.Address2,
        ZipPostalCode = model.ZipPostalCode,
        PhoneNumber = model.PhoneNumber,
        FaxNumber = model.FaxNumber,
    };
}
