using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Common;
using Nop.Web.Areas.Admin.Models.Shipping;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ShippingController
{
    private async Task PrepareWarehouseAddressModelAsync(WarehouseAddressModel model, Address? address)
    {
        model.AvailableCountries.Add(new SelectListItem { Text = "Select country", Value = "0" });
        foreach (var c in await countryService.GetAllCountriesAsync(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString(),
                Selected = address != null && c.Id == address.CountryId
            });

        var countryId = address?.CountryId ?? model.CountryId;
        if (countryId.HasValue && countryId.Value > 0)
        {
            var states = await stateProvinceService.GetStateProvincesByCountryIdAsync(countryId.Value, showHidden: true);
            foreach (var s in states)
                model.AvailableStates.Add(new SelectListItem
                {
                    Text = s.Name,
                    Value = s.Id.ToString(),
                    Selected = address != null && s.Id == address.StateProvinceId
                });
        }

        if (model.AvailableStates.Count == 0)
            model.AvailableStates.Add(new SelectListItem { Text = "Other (Non US)", Value = "0" });

        if (address != null)
        {
            model.CountryId = address.CountryId;
            model.StateProvinceId = address.StateProvinceId;
            model.City = address.City;
            model.Address1 = address.Address1;
            model.ZipPostalCode = address.ZipPostalCode;
            model.PhoneNumber = address.PhoneNumber;
        }
    }
}
