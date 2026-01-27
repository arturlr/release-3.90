using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class CommonController : BasePublicController
    {
        private readonly IAddressService _addressService;
        private readonly ILanguageService _languageService;
        private readonly ICurrencyService _currencyService;

        public CommonController(
            IWorkContext workContext,
            IAddressService addressService,
            ILanguageService languageService,
            ICurrencyService currencyService) : base(workContext)
        {
            _addressService = addressService;
            _languageService = languageService;
            _currencyService = currencyService;
        }

        // GET: /
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        // GET: /ContactUs
        public IActionResult ContactUs()
        {
            return View();
        }

        // POST: /ContactUs
        [HttpPost]
        public IActionResult ContactUs(string email, string subject, string message)
        {
            // TODO: Send email
            return Content("Thank you for contacting us!");
        }

        // GET: /Sitemap
        public IActionResult Sitemap()
        {
            return Content("Sitemap");
        }

        // GET: /PageNotFound
        public IActionResult PageNotFound()
        {
            return NotFound("Page not found");
        }

        // GET: /SetLanguage
        public async Task<IActionResult> SetLanguage(int languageId)
        {
            var language = await _languageService.GetLanguageByIdAsync(languageId);
            if (language != null)
            {
                // TODO: Set language in cookie
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: /SetCurrency
        public async Task<IActionResult> SetCurrency(int currencyId)
        {
            var currency = await _currencyService.GetCurrencyByIdAsync(currencyId);
            if (currency != null)
            {
                // TODO: Set currency in cookie
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: /GetStatesByCountryId
        public async Task<IActionResult> GetStatesByCountryId(int countryId)
        {
            // TODO: Return states as JSON
            return Json(new { states = new List<object>() });
        }
    }
}
