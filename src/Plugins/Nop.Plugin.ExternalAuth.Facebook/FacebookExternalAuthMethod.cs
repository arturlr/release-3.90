using Microsoft.AspNetCore.Routing;
using Nop.Core.Plugins;
using Nop.Services.Authentication.External;
using Nop.Services.Configuration;
using Nop.Services.Localization;

namespace Nop.Plugin.ExternalAuth.Facebook
{
    /// <summary>
    /// Facebook externalAuth processor
    /// </summary>
    /// <remarks>
    /// Task 11.1. One substitution: <c>using System.Web.Routing;</c> →
    /// <c>using Microsoft.AspNetCore.Routing;</c>. <c>RouteValueDictionary</c> lives there now and
    /// task 6.2 already changed the namespace on <c>IExternalAuthenticationMethod</c> itself, so
    /// both <c>Get*Route</c> methods keep 3.90's signatures and bodies exactly — including the
    /// <c>"Namespaces"</c> entry, which is inert in ASP.NET Core (§17.4a) but harmless: it is
    /// carried into the <c>Html.Action</c> bridge's route values, where an unmatched key is simply
    /// visible through <c>RouteData</c> as it was in MVC 5. It is left in place because these are
    /// two of the five dynamically-named <c>Html.Action</c> call sites (deferral 7.3-1) and their
    /// data shape is part of the contract other plugins copy.
    /// <para>
    /// The <c>{ "area", null }</c> entry is now doing real work rather than being decorative:
    /// task 8.8 made the bridge area-aware, and an explicit <c>area</c> in the caller's route
    /// values wins over the ambient one — so this is what sends the bridge looking for a
    /// controller OUTSIDE the Admin area, which is where a plugin's controller is (§77.1).
    /// </para>
    /// </remarks>
    public class FacebookExternalAuthMethod : BasePlugin, IExternalAuthenticationMethod
    {
        #region Fields

        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public FacebookExternalAuthMethod(ISettingService settingService)
        {
            this._settingService = settingService;
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "ExternalAuthFacebook";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.ExternalAuth.Facebook.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for displaying plugin in public store
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPublicInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PublicInfo";
            controllerName = "ExternalAuthFacebook";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.ExternalAuth.Facebook.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new FacebookExternalAuthSettings
            {
                ClientKeyIdentifier = "",
                ClientSecret = "",
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.ExternalAuth.Facebook.ClientKeyIdentifier", "App ID/API Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.ExternalAuth.Facebook.ClientKeyIdentifier.Hint", "Enter your app ID/API key here. You can find it on your FaceBook application page.");
            this.AddOrUpdatePluginLocaleResource("Plugins.ExternalAuth.Facebook.ClientSecret", "App Secret");
            this.AddOrUpdatePluginLocaleResource("Plugins.ExternalAuth.Facebook.ClientSecret.Hint", "Enter your app secret here. You can find it on your FaceBook application page.");

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<FacebookExternalAuthSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.ExternalAuth.Facebook.ClientKeyIdentifier");
            this.DeletePluginLocaleResource("Plugins.ExternalAuth.Facebook.ClientKeyIdentifier.Hint");
            this.DeletePluginLocaleResource("Plugins.ExternalAuth.Facebook.ClientSecret");
            this.DeletePluginLocaleResource("Plugins.ExternalAuth.Facebook.ClientSecret.Hint");

            base.Uninstall();
        }

        #endregion
    }
}
