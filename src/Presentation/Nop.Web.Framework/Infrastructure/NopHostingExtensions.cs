using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Plugins;

namespace Nop.Web.Framework.Infrastructure
{
    /// <summary>
    /// The process-wide state that must be assigned <b>before</b> the service collection is
    /// configured and before the Autofac container is built (task 6.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nop.Core carries three static seams that the migration introduced because their call
    /// sites are static and run before any container exists. Each one is silently wrong until
    /// the host assigns it, and each has to be assigned in a specific order relative to the
    /// rest of startup. Rather than leave task 7.2 to rediscover that ordering, the knowledge
    /// lives here — next to the code that depends on it — and 7.2 calls one method.
    /// This mirrors what task 6.4 did with
    /// <see cref="NopServiceCollectionExtensions.AddNopFramework(Microsoft.Extensions.DependencyInjection.IServiceCollection, bool, Action{Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions}, Action{Microsoft.AspNetCore.Mvc.MvcOptions})"/>
    /// and <c>UseNopPipeline</c>.
    /// </para>
    /// <para>
    /// <b>THE ORDERING IS THE POINT, NOT A DETAIL.</b>
    /// <see cref="CommonHelper.BaseDirectory"/> must be assigned before
    /// <b>anything</b> calls <see cref="CommonHelper.MapPath"/>, and
    /// <c>PluginManager.Initialize()</c> calls it three times on its very first statements
    /// (<c>~/Plugins</c>, <c>~/Plugins/bin</c>, <c>~/App_Data/InstalledPlugins.txt</c>).
    /// Assigning the content root from inside <c>AddNopFramework</c> would therefore be
    /// <b>too late</b>: plugin discovery has to run before <c>AddNopFramework</c> so that
    /// <c>AddPluginApplicationParts</c> has a plugin list to work with, which means plugin
    /// discovery would have shadow-copied every plugin assembly into
    /// <c>bin/Debug/net10.0/Plugins/bin</c> instead of <c>&lt;contentroot&gt;/Plugins/bin</c>.
    /// That is why this is a separate call and not folded into <c>AddNopFramework</c>.
    /// Likewise <c>NopConfigurationManager.Configuration</c> must be assigned before
    /// <c>PluginManager.Initialize()</c>, which reads the
    /// <c>ClearPluginsShadowDirectoryOnStartup</c> app setting through it.
    /// </para>
    /// <para>
    /// The resulting order — content root, then configuration, then plugins — is exactly what
    /// <see cref="UseNopHostingEnvironment"/> performs.
    /// </para>
    /// </remarks>
    public static class NopHostingExtensions
    {
        /// <summary>
        /// Whether <see cref="UseNopHostingEnvironment"/> (or
        /// <see cref="SetContentRoot"/>) has pointed
        /// <see cref="CommonHelper.BaseDirectory"/> at a real content root.
        /// </summary>
        /// <remarks>
        /// <see cref="CommonHelper.BaseDirectory"/> cannot answer this itself: its getter
        /// falls back to <c>AppDomain.CurrentDomain.BaseDirectory</c>, so "unset" and "set to
        /// the output folder" are indistinguishable from outside. This flag exists so the
        /// task 7.7 smoke check can assert that the host did the assignment instead of
        /// discovering wrong paths at runtime. It is deliberately NOT enforced by
        /// <c>AddNopFramework</c>: a host that assigns
        /// <c>CommonHelper.BaseDirectory</c> directly (which is legitimate, and is what the
        /// runtime-deferrals register originally documented) would then fail spuriously.
        /// </remarks>
        public static bool ContentRootConfigured { get; private set; }

        /// <summary>
        /// Assigns every pre-container static seam, in the one order that is correct, and
        /// optionally runs plugin discovery. Call this as the FIRST nopCommerce statement in
        /// <c>Program.cs</c>, before <c>AddNopFramework()</c> and before engine
        /// initialization.
        /// <code>
        /// var builder = WebApplication.CreateBuilder(args);
        /// builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        ///
        /// builder.Environment.UseNopHostingEnvironment(builder.Configuration);   // this method
        ///
        /// builder.Services.AddNopFramework(builder.Configuration);
        /// </code>
        /// </summary>
        /// <param name="environment">
        /// The host environment. <c>WebApplicationBuilder.Environment</c> satisfies this;
        /// the parameter is the broader <see cref="IHostEnvironment"/> rather than
        /// <c>IWebHostEnvironment</c> because only <c>ContentRootPath</c> is needed, which
        /// makes the method usable from a non-web host (background worker, test fixture) too.
        /// </param>
        /// <param name="configuration">
        /// Application configuration. When supplied it is published on
        /// <see cref="NopConfigurationManager.Configuration"/>, which is what makes
        /// <c>NopConfig</c> and the legacy <c>appSettings</c> lookups resolve instead of
        /// falling back to defaults (runtime deferral 1.4). Authoring the matching
        /// <c>appsettings.json</c> sections remains task 7.4's job.
        /// </param>
        /// <param name="initializePlugins">
        /// Whether to run <c>PluginManager.Initialize()</c> (runtime deferral 1.1). Defaults
        /// to <c>true</c> because it MUST happen here — after the content root is set and
        /// before <c>AddNopFramework()</c> adds plugin application parts — and because 3.90
        /// ran it unconditionally via <c>[PreApplicationStartMethod]</c>. Pass <c>false</c>
        /// only for a host that has no plugin directory at all.
        /// </param>
        /// <returns>The environment, so the call can be chained.</returns>
        public static IHostEnvironment UseNopHostingEnvironment(this IHostEnvironment environment,
            IConfiguration configuration = null,
            bool initializePlugins = true)
        {
            if (environment == null)
                throw new ArgumentNullException("environment");

            //runtime deferral 1.5 - must be first, see the class remarks
            SetContentRoot(environment.ContentRootPath);

            //runtime deferral 1.4 - must precede PluginManager.Initialize(), which reads
            //the ClearPluginsShadowDirectoryOnStartup app setting through this seam
            if (configuration != null)
                NopConfigurationManager.Configuration = configuration;

            //runtime deferral 1.1 - replaces the removed
            //[assembly: PreApplicationStartMethod(typeof(PluginManager), "Initialize")]
            if (initializePlugins)
                PluginManager.Initialize();

            return environment;
        }

        /// <summary>
        /// Points <see cref="CommonHelper.MapPath"/> at the web content root instead of the
        /// process output folder (runtime deferral 1.5).
        /// </summary>
        /// <param name="contentRootPath">
        /// Absolute path to the application content root — <c>IHostEnvironment.ContentRootPath</c>.
        /// </param>
        /// <remarks>
        /// <para>
        /// In 3.90 <c>CommonHelper.MapPath</c> delegated to
        /// <c>HostingEnvironment.MapPath</c>, which resolved <c>~/</c> against the web
        /// application root. There is no ambient equivalent in ASP.NET Core, so task 2.4
        /// replaced it with the settable <see cref="CommonHelper.BaseDirectory"/>, whose
        /// default is <c>AppDomain.CurrentDomain.BaseDirectory</c> — i.e.
        /// <c>bin/Debug/net10.0/</c>. Roughly 30 call sites across Nop.Data, Nop.Services and
        /// the presentation projects resolve <c>~/App_Data</c>, <c>~/Plugins</c>,
        /// <c>~/content/images</c> and the install SQL files through it, and every one of them
        /// silently points into the output folder until this is called.
        /// </para>
        /// <para>
        /// Exposed separately from <see cref="UseNopHostingEnvironment"/> for hosts that have
        /// a content-root path but no <see cref="IHostEnvironment"/> — notably test fixtures.
        /// </para>
        /// </remarks>
        public static void SetContentRoot(string contentRootPath)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
                throw new NopException(
                    "The host supplied no content root path, so CommonHelper.MapPath(\"~/...\") " +
                    "would keep resolving relative to the process output directory. Pass " +
                    "IHostEnvironment.ContentRootPath (WebApplicationBuilder.Environment.ContentRootPath).");

            CommonHelper.BaseDirectory = contentRootPath;
            ContentRootConfigured = true;
        }
    }
}
