using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Common;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class AddressController : BasePublicController
    {
        private readonly IAddressService _addressService;

        public AddressController(
            IWorkContext workContext,
            IAddressService addressService) : base(workContext)
        {
            _addressService = addressService;
        }

        // GET: /Address/List
        public async Task<IActionResult> List()
        {
            var addresses = await _addressService.GetAddressesByCustomerIdAsync(CurrentCustomer.Id);
            return View(addresses);
        }

        // GET: /Address/Add
        public IActionResult Add()
        {
            return View();
        }

        // POST: /Address/Add
        [HttpPost]
        public async Task<IActionResult> Add(string firstName, string lastName, string address1, string city, string zip)
        {
            var address = new Core.Domain.Common.Address
            {
                FirstName = firstName,
                LastName = lastName,
                Address1 = address1,
                City = city,
                ZipPostalCode = zip,
                CreatedOnUtc = DateTime.UtcNow
            };

            await _addressService.InsertAddressAsync(address);
            return RedirectToAction("List");
        }

        // GET: /Address/Edit/5
        public async Task<IActionResult> Edit(int addressId)
        {
            var address = await _addressService.GetAddressByIdAsync(addressId);
            if (address == null)
                return NotFound();

            return Content($"Edit Address: {address.Address1}");
        }

        // POST: /Address/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int addressId)
        {
            var address = await _addressService.GetAddressByIdAsync(addressId);
            if (address == null)
                return NotFound();

            await _addressService.DeleteAddressAsync(address);
            return RedirectToAction("List");
        }
    }
}
