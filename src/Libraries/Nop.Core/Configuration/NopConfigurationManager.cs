using Microsoft.Extensions.Configuration;

namespace Nop.Core.Configuration
{
    /// <summary>
    /// Static access point to the application's configuration for the code paths that used to
    /// call <c>System.Configuration.ConfigurationManager</c>.
    /// </summary>
    /// <remarks>
    /// Task 2.4 (design section 3, Requirement 1.4).
    /// <c>System.Configuration.ConfigurationManager</c> is not part of the .NET / ASP.NET Core
    /// shared framework, and <c>app.config</c>/<c>web.config</c> are no longer the
    /// configuration source, so the remaining <c>ConfigurationManager.AppSettings[...]</c> and
    /// <c>ConfigurationManager.GetSection("NopConfig")</c> call sites in Nop.Core
    /// (<c>WebHelper</c>, <c>Plugins/PluginManager</c>, <c>Infrastructure/EngineContext</c>)
    /// are routed through here instead.
    ///
    /// The three call sites are static or run before the container exists, so they cannot take
    /// an injected <see cref="IConfiguration"/>; this holder is the seam that lets the host
    /// supply one. Everything that is constructed through DI should keep injecting
    /// <see cref="IConfiguration"/> / <c>IOptions&lt;NopConfig&gt;</c> directly and must not use
    /// this class.
    ///
    /// RUNTIME DEFERRAL: <see cref="Configuration"/> is assigned by the ASP.NET Core host at
    /// startup (tasks 7.2 / 7.4). Until then it is null, and every accessor below returns its
    /// documented fallback - legacy <c>appSettings</c> lookups return null (which is exactly
    /// what <c>ConfigurationManager.AppSettings</c> returned for a missing key) and
    /// <see cref="GetNopConfig"/> returns an all-default <see cref="NopConfig"/>.
    /// </remarks>
    public static class NopConfigurationManager
    {
        /// <summary>
        /// The application configuration. Set once during host startup (tasks 7.2 / 7.4).
        /// </summary>
        public static IConfiguration Configuration { get; set; }

        /// <summary>
        /// The name of the configuration section that replaces the legacy
        /// <c>&lt;appSettings&gt;</c> element.
        /// </summary>
        public const string AppSettingsSectionName = "appSettings";

        /// <summary>
        /// Gets a legacy application setting by name, or null when it is not configured
        /// </summary>
        /// <param name="name">Setting name, as it appeared in <c>&lt;appSettings&gt;</c></param>
        /// <returns>Setting value, or null</returns>
        public static string GetAppSetting(string name)
        {
            if (Configuration == null || string.IsNullOrEmpty(name))
                return null;

            //prefer the "appSettings" section (a direct translation of the legacy element),
            //then fall back to a root-level key
            var value = Configuration[AppSettingsSectionName + ":" + name];
            if (string.IsNullOrEmpty(value))
                value = Configuration[name];

            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// Gets the <see cref="NopConfig"/> options, bound from the
        /// <see cref="NopConfig.SectionName"/> configuration section
        /// </summary>
        /// <returns>
        /// The bound configuration; an all-default instance when no configuration source has
        /// been supplied yet. Never null - the legacy
        /// <c>ConfigurationManager.GetSection("NopConfig") as NopConfig</c> could return null,
        /// which made <c>NopEngine.Initialize</c> throw; returning defaults keeps startup and
        /// unit tests working.
        /// </returns>
        public static NopConfig GetNopConfig()
        {
            var config = new NopConfig();

            if (Configuration != null)
                Configuration.GetSection(NopConfig.SectionName).Bind(config);

            return config;
        }
    }
}
