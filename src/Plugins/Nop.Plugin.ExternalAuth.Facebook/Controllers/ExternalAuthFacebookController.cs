using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Plugins;
using Nop.Plugin.ExternalAuth.Facebook.Core;
using Nop.Plugin.ExternalAuth.Facebook.Models;
using Nop.Services.Authentication.External;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.ExternalAuth.Facebook.Controllers
{
    /// <remarks>
    /// Task 11.1 substitutions — all of them decisions tasks 7.3 (§30), 8.3 (§55) and 10.x
    /// already made and recorded:
    /// <list type="bullet">
    /// <item><c>using System.Web.Mvc;</c> → <c>using Microsoft.AspNetCore.Mvc;</c>.</item>
    /// <item><c>[ChildActionOnly]</c> → <c>[NopChildActionOnly]</c> (deferral 7.3-4, resolved at
    /// 8.3). The attribute has no ASP.NET Core counterpart; the marker plus
    /// <c>NopChildActionOnlyConvention</c> — registered by <c>AddNopFramework</c>, so a plugin
    /// gets both halves for free — removes the action from INBOUND route matching while leaving
    /// it invocable through the <c>Html.Action</c> bridge. Without it,
    /// <c>GET /ExternalAuthFacebook/Configure</c> would return the bare admin settings panel over
    /// the <c>Default</c> route. <b>This is the first PLUGIN use of that marker</b>; the 48
    /// storefront and 32 admin sites were done at 7.3/8.3.</item>
    /// <item><c>[NonAction]</c> on <c>LoginInternal</c> — <b>kept</b>. It exists in ASP.NET Core
    /// with the same meaning, and it is load-bearing: the method is <c>private</c>, but 3.90
    /// marked it and MVC's action discovery would otherwise be the only thing standing between a
    /// future refactor and a routable <c>LoginInternal</c>.</item>
    /// <item><c>TryUpdateModel(viewModel)</c> → <c>TryUpdateModelAsync(viewModel)</c> awaited
    /// synchronously. ASP.NET Core has no synchronous overload. The call is 3.90's and is
    /// <b>pointless in both versions</b> — <c>viewModel</c> is a local that nothing reads
    /// afterwards — but removing it would be a behavioural change (it populates
    /// <c>ModelState</c>, which <c>ExternalAuthorizer</c> does not consult but a future filter
    /// might), so it is preserved verbatim in shape.</item>
    /// <item><c>HttpContext.Request.IsAuthenticated</c> →
    /// <c>User.Identity != null &amp;&amp; User.Identity.IsAuthenticated</c>.
    /// <c>HttpRequest.IsAuthenticated</c> was a <c>System.Web</c> convenience over the same
    /// principal; the null test is new only because <c>ClaimsPrincipal.Identity</c> is nullable in
    /// ASP.NET Core, and it fails in the same direction 3.90 did (unauthenticated → redirect to
    /// log on).</item>
    /// <item><c>Content("Access denied")</c>, <c>ActionResult</c>, <c>[HttpPost]</c>,
    /// <c>RedirectToRoute</c>, <c>new RedirectResult(...)</c>, <c>Url.LogOn(returnUrl)</c> (an
    /// <c>IUrlHelper</c> extension after task 6.3) and
    /// <c>this.GetActiveStoreScopeConfiguration(...)</c> needed <b>no</b> edit.</item>
    /// <item><c>View("~/Plugins/ExternalAuth.Facebook/Views/….cshtml", model)</c> — <b>both call
    /// sites UNCHANGED.</b> The project file's <c>Content</c>/<c>Link</c> block makes the compiled
    /// Razor identifiers equal these paths.</item>
    /// </list>
    /// <para>
    /// <b>The <c>[HttpPost] Configure(ConfigurationModel)</c> overload is the reason task 11.1
    /// had to fix the <c>Html.Action</c> bridge (runtime deferral 11.x-1).</b> This page is
    /// rendered as a child action from <c>Areas/Admin/Views/ExternalAuthentication/
    /// ConfigureMethod.cshtml</c>, and <c>Html.BeginForm()</c> posts back to that admin URL — so
    /// the POST arrives as another child-action render, and the bridge has to select the
    /// <c>[HttpPost]</c> overload and model-bind it from the form. It did neither before 11.1,
    /// which meant this form silently discarded every change.
    /// </para>
    /// </remarks>
    public class ExternalAuthFacebookController : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly IOAuthProviderFacebookAuthorizer _oAuthProviderFacebookAuthorizer;
        private readonly IOpenAuthenticationService _openAuthenticationService;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IPluginFinder _pluginFinder;
        private readonly ILocalizationService _localizationService;

        public ExternalAuthFacebookController(ISettingService settingService,
            IOAuthProviderFacebookAuthorizer oAuthProviderFacebookAuthorizer,
            IOpenAuthenticationService openAuthenticationService,
            ExternalAuthenticationSettings externalAuthenticationSettings,
            IPermissionService permissionService,
            IStoreContext storeContext,
            IStoreService storeService,
            IWorkContext workContext,
            IPluginFinder pluginFinder,
            ILocalizationService localizationService)
        {
            this._settingService = settingService;
            this._oAuthProviderFacebookAuthorizer = oAuthProviderFacebookAuthorizer;
            this._openAuthenticationService = openAuthenticationService;
            this._externalAuthenticationSettings = externalAuthenticationSettings;
            this._permissionService = permissionService;
            this._storeContext = storeContext;
            this._storeService = storeService;
            this._workContext = workContext;
            this._pluginFinder = pluginFinder;
            this._localizationService = localizationService;
        }
        
        [AdminAuthorize]
        [NopChildActionOnly]
        public ActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageExternalAuthenticationMethods))
                return Content("Access denied");

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var facebookExternalAuthSettings = _settingService.LoadSetting<FacebookExternalAuthSettings>(storeScope);

            var model = new ConfigurationModel();
            model.ClientKeyIdentifier = facebookExternalAuthSettings.ClientKeyIdentifier;
            model.ClientSecret = facebookExternalAuthSettings.ClientSecret;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.ClientKeyIdentifier_OverrideForStore = _settingService.SettingExists(facebookExternalAuthSettings, x => x.ClientKeyIdentifier, storeScope);
                model.ClientSecret_OverrideForStore = _settingService.SettingExists(facebookExternalAuthSettings, x => x.ClientSecret, storeScope);
            }

            return View("~/Plugins/ExternalAuth.Facebook/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [NopChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageExternalAuthenticationMethods))
                return Content("Access denied");

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var facebookExternalAuthSettings = _settingService.LoadSetting<FacebookExternalAuthSettings>(storeScope);

            //save settings
            facebookExternalAuthSettings.ClientKeyIdentifier = model.ClientKeyIdentifier;
            facebookExternalAuthSettings.ClientSecret = model.ClientSecret;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(facebookExternalAuthSettings, x => x.ClientKeyIdentifier, model.ClientKeyIdentifier_OverrideForStore , storeScope, false);
            _settingService.SaveSettingOverridablePerStore(facebookExternalAuthSettings, x => x.ClientSecret, model.ClientSecret_OverrideForStore, storeScope, false);
           
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [NopChildActionOnly]
        public ActionResult PublicInfo()
        {
            return View("~/Plugins/ExternalAuth.Facebook/Views/PublicInfo.cshtml");
        }

        [NonAction]
        private ActionResult LoginInternal(string returnUrl, bool verifyResponse)
        {
            var processor = _openAuthenticationService.LoadExternalAuthenticationMethodBySystemName("ExternalAuth.Facebook");
            if (processor == null ||
                !processor.IsMethodActive(_externalAuthenticationSettings) ||
                !processor.PluginDescriptor.Installed ||
                !_pluginFinder.AuthenticateStore(processor.PluginDescriptor, _storeContext.CurrentStore.Id) ||
                !_pluginFinder.AuthorizedForUser(processor.PluginDescriptor, _workContext.CurrentCustomer))
                throw new NopException("Facebook module cannot be loaded");

            var viewModel = new LoginModel();
            TryUpdateModelAsync(viewModel).GetAwaiter().GetResult();

            var result = _oAuthProviderFacebookAuthorizer.Authorize(returnUrl, verifyResponse);
            switch (result.AuthenticationStatus)
            {
                case OpenAuthenticationStatus.Error:
                    {
                        if (!result.Success)
                            foreach (var error in result.Errors)
                                ExternalAuthorizerHelper.AddErrorsToDisplay(error);

                        return new RedirectResult(Url.LogOn(returnUrl));
                    }
                case OpenAuthenticationStatus.AssociateOnLogon:
                    {
                        return new RedirectResult(Url.LogOn(returnUrl));
                    }
                case OpenAuthenticationStatus.AutoRegisteredEmailValidation:
                    {
                        //result
                        return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation });
                    }
                case OpenAuthenticationStatus.AutoRegisteredAdminApproval:
                    {
                        return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval });
                    }
                case OpenAuthenticationStatus.AutoRegisteredStandard:
                    {
                        return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Standard });
                    }
                default:
                    break;
            }

            if (result.Result != null) return result.Result;
            var isAuthenticated = User != null && User.Identity != null && User.Identity.IsAuthenticated;
            return isAuthenticated ? new RedirectResult(!string.IsNullOrEmpty(returnUrl) ? returnUrl : "~/") : new RedirectResult(Url.LogOn(returnUrl));
        }
        
        public ActionResult Login(string returnUrl)
        {
            return LoginInternal(returnUrl, false);
        }

        public ActionResult LoginCallback(string returnUrl)
        {
            return LoginInternal(returnUrl, true);
        }
    }
}