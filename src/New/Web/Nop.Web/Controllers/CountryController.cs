using Microsoft.AspNetCore.Mvc;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers;

/// <summary>
/// AJAX endpoint for state/province lookup used by address forms.
/// Accessible even when store navigation is restricted (no PublicStoreAllowNavigationFilter).
/// </summary>
public class CountryController(
    IStateProvinceService stateProvinceService,
    ILocalizationService localizationService) : BasePublicController
{
    [HttpGet]
    public async Task<IActionResult> GetStatesByCountryId(string countryId, bool addSelectStateItem)
    {
        if (!int.TryParse(countryId, out var id))
            id = 0;

        var states = await stateProvinceService.GetStateProvincesByCountryIdAsync(id);

        var result = states
            .Select(s => new { id = s.Id, name = s.Name })
            .ToList<object>();

        if (states.Count == 0)
        {
            // No states — show "Other (Non US)" or "Select state" placeholder
            result.Insert(0, new
            {
                id = 0,
                name = addSelectStateItem
                    ? await localizationService.GetResourceAsync("Address.SelectState")
                    : await localizationService.GetResourceAsync("Address.OtherNonUS")
            });
        }
        else if (addSelectStateItem)
        {
            result.Insert(0, new { id = 0, name = await localizationService.GetResourceAsync("Address.SelectState") });
        }

        return Json(result);
    }
}
