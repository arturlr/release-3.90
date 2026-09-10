using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Plugins;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Themes;

namespace Nop.Web.Framework.Infrastructure
{
    /// <summary>
    /// Everything <c>Nop.Web.Framework</c> needs on the <see cref="IServiceCollection"/>,
    /// collected here so task 7.2's <c>Program.cs</c> is one call instead of a rediscovery
    /// exercise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this lives in this project and not in <c>Program.cs</c>.</b> nopCommerce's
    /// <c>IDependencyRegistrar</c> receives an Autofac <c>ContainerBuilder</c>, not an
    /// <see cref="IServiceCollection"/>, so none of the registrations below can go into
    /// <c>DependencyRegistrar</c>. They are <see cref="IServiceCollection"/> concerns and
    /// therefore belong to task 7.2 — but the knowledge of <i>which</i> registrations are
    /// required, and why, belongs where the types live. Task 7.2 calls
    /// <see cref="AddNopFramework"/>; it does not have to know the list.
    /// </para>
    /// <para>
    /// <b>Runtime deferrals this closes</b> (they become "done" the moment 7.2 calls it):
    /// 1.2 (plugin assemblies invisible to the Razor compiler — application parts),
    /// 3/7.14 (<c>IHttpContextAccessor</c>), 3 (<c>IMemoryCache</c> for
    /// <c>MemoryCacheManager</c>), 7.13 (cookie authentication), 7.15 (session),
    /// 11.20 (FluentValidation model-validator provider), 11.21 (<c>NopMetadataProvider</c>),
    /// 11.22 (<c>NopModelBinderProvider</c> / string trimming),
    /// 11.23 (<c>JsonResult</c> PascalCase), 11.26 (<c>IAntiforgery</c>),
    /// 11.29/29 (forwarded headers), 30 (theming view-location expander),
    /// 31 (<c>IFileVersionProvider</c> for <c>PageHeadBuilder</c>).
    /// </para>
    /// <para>
    /// <b>Runtime deferral 1.6 (<c>IHostApplicationLifetime</c>, needed by
    /// <c>WebHelper.RestartAppDomain</c>) needs nothing here.</b> The .NET generic host
    /// registers <c>IHostApplicationLifetime</c> on the <see cref="IServiceCollection"/>
    /// itself, and <c>Autofac.Extensions.DependencyInjection</c>'s <c>Populate</c> copies it
    /// into the nopCommerce container — so it is resolvable as soon as task 7.2 uses
    /// <c>AutofacServiceProviderFactory</c>. The only requirement is that the process is run
    /// under a supervisor that restarts it (ANCM / systemd / container orchestrator).
    /// </para>
    /// <para>
    /// <b>Runtime deferrals 1.1, 1.4 and 1.5 are NOT closed here, and must not be.</b> They are
    /// process-wide static seams that have to be assigned strictly BEFORE this method runs —
    /// see <see cref="NopHostingExtensions"/>, which is the one call that does them in the
    /// right order.
    /// </para>
    /// </remarks>
    public static class NopServiceCollectionExtensions
    {
        /// <summary>
        /// Default authentication cookie name — 3.90's <c>&lt;forms name="NOPCOMMERCE.AUTH"&gt;</c>.
        /// </summary>
        public const string AuthenticationCookieName = "NOPCOMMERCE.AUTH";

        /// <summary>
        /// Default login path — 3.90's <c>&lt;forms loginUrl="~/login"&gt;</c>.
        /// </summary>
        public const string DefaultLoginPath = "/login";

        /// <summary>
        /// Default forms-auth lifetime — 3.90's <c>&lt;forms timeout="43200"&gt;</c> (minutes,
        /// i.e. 30 days). This value MUST be set on the handler, because
        /// <c>FormsAuthenticationService</c> has no ambient <c>FormsAuthentication.Timeout</c>
        /// to read and falls back to a 30-minute default (see runtime deferral 7.13).
        /// </summary>
        public const int DefaultExpireMinutes = 43200;

        /// <summary>
        /// Registers everything <c>Nop.Web.Framework</c> requires, taking the
        /// <c>&lt;forms&gt;</c>-equivalent authentication settings from configuration
        /// (task 6.5, Requirements 1.4, 5.3), and returns the <see cref="IMvcBuilder"/> so the
        /// host can keep chaining.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the overload task 7.2 should call. It binds
        /// <see cref="NopAuthenticationConfig"/> from the
        /// <see cref="NopAuthenticationConfig.SectionName"/> section of
        /// <paramref name="configuration"/> and also registers it for
        /// <c>IOptions&lt;NopAuthenticationConfig&gt;</c> injection. A missing section is not an
        /// error — every value falls back to 3.90's shipped <c>Web.config</c> default. This is
        /// what removes the hardcoded <c>requireSsl</c> literal from <c>Program.cs</c>, which
        /// runtime deferral 7.13 flags as a security hazard.
        /// </para>
        /// <para>
        /// It does NOT assign <c>NopConfigurationManager.Configuration</c> or
        /// <c>CommonHelper.BaseDirectory</c>: those must be set strictly earlier — see
        /// <see cref="NopHostingExtensions.UseNopHostingEnvironment"/> for why, and call that
        /// first.
        /// </para>
        /// </remarks>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">
        /// Application configuration, e.g. <c>WebApplicationBuilder.Configuration</c>
        /// </param>
        /// <param name="configureCookie">
        /// Optional hook applied AFTER the configured values, so the host can override
        /// <c>Cookie.Domain</c>, <c>DataProtectionProvider</c>, etc.
        /// </param>
        /// <param name="configureMvc">Optional hook on the MVC options.</param>
        public static IMvcBuilder AddNopFramework(this IServiceCollection services,
            IConfiguration configuration,
            Action<CookieAuthenticationOptions> configureCookie = null,
            Action<MvcOptions> configureMvc = null)
        {
            if (services == null)
                throw new ArgumentNullException("services");
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            var authenticationConfig = new NopAuthenticationConfig();
            var section = configuration.GetSection(NopAuthenticationConfig.SectionName);
            section.Bind(authenticationConfig);

            //make the same values injectable as IOptions<NopAuthenticationConfig>
            services.Configure<NopAuthenticationConfig>(section);

            return services.AddNopFramework(authenticationConfig, configureCookie, configureMvc);
        }

        /// <summary>
        /// Registers everything <c>Nop.Web.Framework</c> requires, and returns the
        /// <see cref="IMvcBuilder"/> so the host can keep chaining.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="requireSsl">
        /// 3.90's <c>&lt;forms requireSSL&gt;</c>. <c>false</c> matches the shipped
        /// <c>Web.config</c>. Pass <c>true</c> in any deployment that terminates TLS —
        /// leaving it <c>false</c> when 3.90 had it <c>true</c> is a security downgrade
        /// (runtime deferral 7.13). Prefer the
        /// <see cref="AddNopFramework(IServiceCollection, IConfiguration, Action{CookieAuthenticationOptions}, Action{MvcOptions})"/>
        /// overload, which reads this from configuration instead of hardcoding it.
        /// </param>
        /// <param name="configureCookie">
        /// Optional hook applied AFTER the 3.90-equivalent defaults, so the host can override
        /// <c>Cookie.Domain</c>, <c>DataProtectionProvider</c>, etc.
        /// </param>
        /// <param name="configureMvc">Optional hook on the MVC options.</param>
        public static IMvcBuilder AddNopFramework(this IServiceCollection services,
            bool requireSsl = false,
            Action<CookieAuthenticationOptions> configureCookie = null,
            Action<MvcOptions> configureMvc = null)
        {
            if (services == null)
                throw new ArgumentNullException("services");

            var authenticationConfig = new NopAuthenticationConfig();
            authenticationConfig.RequireSsl = requireSsl;

            return services.AddNopFramework(authenticationConfig, configureCookie, configureMvc);
        }

        /// <summary>
        /// Registers everything <c>Nop.Web.Framework</c> requires from an explicit
        /// <see cref="NopAuthenticationConfig"/>, and returns the <see cref="IMvcBuilder"/> so
        /// the host can keep chaining. Both other overloads funnel through this one.
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="authenticationConfig">
        /// The <c>&lt;forms&gt;</c>-equivalent settings. <c>null</c> means "3.90 defaults".
        /// </param>
        /// <param name="configureCookie">
        /// Optional hook applied AFTER <paramref name="authenticationConfig"/>.
        /// </param>
        /// <param name="configureMvc">Optional hook on the MVC options.</param>
        public static IMvcBuilder AddNopFramework(this IServiceCollection services,
            NopAuthenticationConfig authenticationConfig,
            Action<CookieAuthenticationOptions> configureCookie = null,
            Action<MvcOptions> configureMvc = null)
        {
            if (services == null)
                throw new ArgumentNullException("services");

            var auth = authenticationConfig ?? new NopAuthenticationConfig();

            //--------------------------------------------------------------------------
            // HTTP context + caches
            //   IHttpContextAccessor: required by Nop.Core's WebHelper and
            //   PerRequestCacheManager, by four Nop.Services types, by WebWorkContext,
            //   RemotePost and PageHeadBuilder (deferrals 3, 7.14, 14.31).
            //   IMemoryCache: MemoryCacheManager(IMemoryCache) (deferral 3).
            //--------------------------------------------------------------------------
            services.AddHttpContextAccessor();
            services.AddMemoryCache();

            //--------------------------------------------------------------------------
            // Session — ExternalAuthorizerHelper parks OpenAuthenticationParameters across
            // the redirect to the external provider (deferral 7.15). A distributed store is
            // required for multi-instance deployments; the in-memory one is the 3.90
            // equivalent of the default <sessionState mode="InProc">.
            //--------------------------------------------------------------------------
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.Cookie.Name = ".Nop.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            //--------------------------------------------------------------------------
            // Cookie authentication — replaces System.Web forms authentication
            // (deferral 7.13). Values come from NopAuthenticationConfig, whose defaults
            // mirror Nop.Web/Web.config's <forms> element:
            //   name="NOPCOMMERCE.AUTH" loginUrl="~/login" timeout="43200" path="/"
            //   requireSSL="false" slidingExpiration="true"
            // Task 6.5 moved them out of hardcoded constants and into IConfiguration
            // (Requirements 1.4, 5.3); the constants below remain as the defaults.
            //--------------------------------------------------------------------------
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = string.IsNullOrEmpty(auth.CookieName)
                        ? AuthenticationCookieName
                        : auth.CookieName;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.Path = string.IsNullOrEmpty(auth.CookiePath) ? "/" : auth.CookiePath;
                    options.Cookie.SecurePolicy = auth.RequireSsl
                        ? CookieSecurePolicy.Always
                        : CookieSecurePolicy.SameAsRequest;
                    options.Cookie.IsEssential = true;

                    var loginPath = string.IsNullOrEmpty(auth.LoginPath) ? DefaultLoginPath : auth.LoginPath;
                    options.LoginPath = loginPath;
                    options.AccessDeniedPath = loginPath;

                    options.ExpireTimeSpan = TimeSpan.FromMinutes(
                        auth.TimeoutMinutes > 0 ? auth.TimeoutMinutes : DefaultExpireMinutes);
                    options.SlidingExpiration = auth.SlidingExpiration;

                    if (configureCookie != null)
                        configureCookie(options);
                });

            //XSRF filters call IAntiforgery.ValidateRequestAsync and use GetRequiredService,
            //i.e. they fail closed if it is missing (deferral 11.26). AddControllersWithViews
            //registers it implicitly, but be explicit so the dependency is visible.
            services.AddAntiforgery();

            //IsCurrentConnectionSecured() reads HttpRequest.IsHttps, which is false behind a
            //TLS-terminating proxy - and ForceSslForAllPages would then loop (deferral 11.29).
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                //a reverse proxy in front of the app is not in a known network by default.
                //KnownNetworks (List<IPNetwork> of the internal type) is obsolete as of
                //ASPDEPR005; KnownIPNetworks (System.Net.IPNetwork) is the replacement.
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            //--------------------------------------------------------------------------
            // MVC
            //--------------------------------------------------------------------------
            var mvcBuilder = services.AddControllersWithViews(options =>
            {
                //deferral 11.20 - FluentValidation is nopCommerce's server-side validation for
                //login, registration, change-password, checkout and every admin form. Without
                //this, ModelState.IsValid returns true for input those validators reject.
                options.ModelValidatorProviders.Add(
                    new NopFluentValidationModelValidatorProvider(new NopValidatorFactory()));

                //deferral 11.21 - NopResourceDisplayName / AdditionalInfo in ModelMetadata.AdditionalValues
                options.ModelMetadataDetailsProviders.Add(new NopMetadataProvider());

                //deferral 11.22 - trims submitted strings except [NoTrim] members
                options.ModelBinderProviders.Insert(0, new NopModelBinderProvider());

                if (configureMvc != null)
                    configureMvc(options);
            });

            //deferral 11.23 - MVC 5's JavaScriptSerializer emitted property names exactly as
            //declared. System.Text.Json camel-cases by default, which breaks every Kendo grid
            //script reading PascalCase fields and DataSourceResult's Data/Total/Errors/ExtraData.
            //Null policy == "use the declared names" == 3.90 parity, with no extra package.
            mvcBuilder.AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                options.JsonSerializerOptions.DictionaryKeyPolicy = null;
            });

            //Controllers are registered in the Autofac container by DependencyRegistrar
            //(reproducing Autofac.Mvc5's RegisterControllers). This makes MVC actually activate
            //them through the container, which is also what keeps
            //EngineContext.Current.Resolve<SomeController>() working.
            mvcBuilder.AddControllersAsServices();

            //deferral 1.2 - plugin assemblies must be application parts or their controllers
            //and Razor views are invisible to MVC. Requires PluginManager.Initialize() to have
            //run already (deferral 1.1) - call it BEFORE this method.
            mvcBuilder.ConfigureApplicationPartManager(AddPluginApplicationParts);

            //deferral 30 (HIGHEST) - without this every theme is ignored and, worse, the
            //~/Administration/Views/... locations are never searched, so the admin UI 404s on
            //view lookup. Inserted at index 0: ASP.NET Core applies expanders in order and
            //this one must see the unexpanded locations.
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Insert(0, new ThemeableViewLocationExpander());
            });

            return mvcBuilder;
        }

        /// <summary>
        /// Contributes every loaded plugin assembly as an MVC application part so its
        /// controllers, view components and Razor views are discoverable (deferral 1.2).
        /// </summary>
        /// <remarks>
        /// Silent no-op when <see cref="PluginManager.ReferencedPlugins"/> is null, which is
        /// the case until <c>PluginManager.Initialize()</c> is called (deferral 1.1, owned by
        /// task 7.2). Call order in <c>Program.cs</c> must be
        /// <c>PluginManager.Initialize()</c> → <c>AddNopFramework()</c>.
        /// </remarks>
        public static void AddPluginApplicationParts(ApplicationPartManager partManager)
        {
            if (partManager == null)
                return;

            var referencedPlugins = PluginManager.ReferencedPlugins;
            if (referencedPlugins == null)
                return;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in partManager.ApplicationParts)
                seen.Add(part.Name);

            foreach (var plugin in referencedPlugins)
            {
                Assembly assembly = plugin == null ? null : plugin.ReferencedAssembly;
                if (assembly == null)
                    continue;

                var name = assembly.GetName().Name;
                if (!seen.Add(name))
                    continue;

                partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            }
        }
    }
}
