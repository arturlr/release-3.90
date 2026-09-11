using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.CheckMoneyOrder.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.CheckMoneyOrder.Controllers
{
    /// <remarks>
    /// Task 12.1 substitutions, all reusing decisions tasks 7.3 (§30), 8.3 (§55) and 10.1
    /// (§83.9) already recorded — nothing here was re-derived:
    /// <list type="bullet">
    /// <item><c>using System.Web.Mvc;</c> → <c>Microsoft.AspNetCore.Mvc</c> +
    /// <c>Microsoft.AspNetCore.Http</c> (for <see cref="IFormCollection"/>).</item>
    /// <item><c>FormCollection</c> → <see cref="IFormCollection"/> on the two
    /// <see cref="BasePaymentController"/> overrides. That is a PUBLIC SIGNATURE CHANGE made
    /// by task 6.2 on the base class, not a choice made here — see the remarks on
    /// <c>Nop.Web.Framework.Controllers.BasePaymentController</c>. Both bodies ignore the
    /// argument, so nothing else changes.</item>
    /// <item><c>[ChildActionOnly]</c> ×3 DELETED — no ASP.NET Core counterpart (the attribute
    /// existed only to make an action non-URL-reachable). This is deferral <b>7.3-4</b>'s
    /// relaxation, recorded there at 48 Nop.Web actions and 16 admin ones: <c>Configure</c>
    /// and <c>PaymentInfo</c> are now also reachable as
    /// <c>/PaymentCheckMoneyOrder/Configure</c> via the <c>Default</c> route. <c>Configure</c>
    /// keeps <c>[AdminAuthorize]</c>, so it is not newly exposed; <c>PaymentInfo</c> renders a
    /// settings-driven description block with no customer data in it. The actions must stay
    /// invocable because <c>IPaymentMethod</c>'s action/controller/<c>RouteValueDictionary</c>
    /// triple is how the host reaches them (deferral 7.3-1) — that is the whole reason the
    /// <c>Html.Action</c> bridge exists and could not be replaced by view components.</item>
    /// <item><c>View("~/Plugins/Payments.CheckMoneyOrder/Views/…")</c> is <b>UNCHANGED</b> —
    /// the point of the <c>Content</c>/<c>Link</c> block in the project file.</item>
    /// </list>
    /// <c>ActionResult</c>, <c>[HttpPost]</c>, <c>[NonAction]</c>, <c>ModelState.IsValid</c>,
    /// <c>[AdminAuthorize]</c>, <c>SuccessNotification</c>, <c>GetActiveStoreScopeConfiguration</c>
    /// and <c>AddLocales</c> all exist with the same shapes and needed no edit.
    /// </remarks>
    public class PaymentCheckMoneyOrderController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;

        public PaymentCheckMoneyOrderController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            ILanguageService languageService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._localizationService = localizationService;
            this._languageService = languageService;
        }
        
        [AdminAuthorize]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var checkMoneyOrderPaymentSettings = _settingService.LoadSetting<CheckMoneyOrderPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.DescriptionText = checkMoneyOrderPaymentSettings.DescriptionText;
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.DescriptionText = checkMoneyOrderPaymentSettings.GetLocalizedSetting(x => x.DescriptionText, languageId, 0, false, false);
            });
            model.AdditionalFee = checkMoneyOrderPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = checkMoneyOrderPaymentSettings.AdditionalFeePercentage;
            model.ShippableProductRequired = checkMoneyOrderPaymentSettings.ShippableProductRequired;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.DescriptionText_OverrideForStore = _settingService.SettingExists(checkMoneyOrderPaymentSettings, x => x.DescriptionText, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(checkMoneyOrderPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(checkMoneyOrderPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.ShippableProductRequired_OverrideForStore = _settingService.SettingExists(checkMoneyOrderPaymentSettings, x => x.ShippableProductRequired, storeScope);
            }

            return View("~/Plugins/Payments.CheckMoneyOrder/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var checkMoneyOrderPaymentSettings = _settingService.LoadSetting<CheckMoneyOrderPaymentSettings>(storeScope);

            //save settings
            checkMoneyOrderPaymentSettings.DescriptionText = model.DescriptionText;
            checkMoneyOrderPaymentSettings.AdditionalFee = model.AdditionalFee;
            checkMoneyOrderPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            checkMoneyOrderPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(checkMoneyOrderPaymentSettings, x => x.DescriptionText, model.DescriptionText_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(checkMoneyOrderPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(checkMoneyOrderPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(checkMoneyOrderPaymentSettings, x => x.ShippableProductRequired, model.ShippableProductRequired_OverrideForStore, storeScope, false);
           
            //now clear settings cache
            _settingService.ClearCache();

            //localization. no multi-store support for localization yet.
            foreach (var localized in model.Locales)
            {
                checkMoneyOrderPaymentSettings.SaveLocalizedSetting(x => x.DescriptionText,
                    localized.LanguageId,
                    localized.DescriptionText);
            }

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        public ActionResult PaymentInfo()
        {
            var checkMoneyOrderPaymentSettings = _settingService.LoadSetting<CheckMoneyOrderPaymentSettings>(_storeContext.CurrentStore.Id);

            var model = new PaymentInfoModel
            {
                DescriptionText = checkMoneyOrderPaymentSettings.GetLocalizedSetting(x => x.DescriptionText, _workContext.WorkingLanguage.Id, _storeContext.CurrentStore.Id)
            };

            return View("~/Plugins/Payments.CheckMoneyOrder/Views/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }
    }
}