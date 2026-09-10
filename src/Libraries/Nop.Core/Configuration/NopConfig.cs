namespace Nop.Core.Configuration
{
    /// <summary>
    /// Represents a NopConfig
    /// </summary>
    /// <remarks>
    /// Migrated from the classic <c>System.Configuration.IConfigurationSectionHandler</c>
    /// mechanism (task 2.3, Requirement 1.4). The type has no net10.0 counterpart, so this
    /// class is now a plain options POCO that binds from any <c>IConfiguration</c> source -
    /// for example:
    /// <code>
    /// services.Configure&lt;NopConfig&gt;(configuration.GetSection(NopConfig.SectionName));
    /// // or, for the non-DI startup path:
    /// var config = new NopConfig();
    /// configuration.GetSection(NopConfig.SectionName).Bind(config);
    /// </code>
    /// Every setting name and type is unchanged from the section-handler version so existing
    /// consumers (<c>RedisCacheManager</c>, <c>RedisConnectionWrapper</c>, <c>NopEngine</c>,
    /// <c>UserAgentHelper</c>, <c>AzurePictureService</c>, <c>Task</c>, and the Nop.Web /
    /// Nop.Admin controllers) compile and behave unchanged. Property setters were widened
    /// from <c>private set</c> to <c>set</c> because <c>ConfigurationBinder</c> only assigns
    /// publicly settable properties.
    ///
    /// Producing the matching <c>appsettings.json</c> section and registering the binding is
    /// owned by task 7.4 (web.config -> appsettings.json / IConfiguration); this class
    /// intentionally contains no configuration-source knowledge of its own.
    /// </remarks>
    public partial class NopConfig
    {
        /// <summary>
        /// The configuration section this options class binds from. Matches the legacy
        /// <c>configSections</c> name used in Web.config / App.config.
        /// </summary>
        public const string SectionName = "NopConfig";

        /// <summary>
        /// Indicates whether we should ignore startup tasks
        /// </summary>
        public bool IgnoreStartupTasks { get; set; }

        /// <summary>
        /// Path to database with user agent strings
        /// </summary>
        public string UserAgentStringsPath { get; set; }

        /// <summary>
        /// Path to database with crawler only user agent strings
        /// </summary>
        public string CrawlerOnlyUserAgentStringsPath { get; set; }



        /// <summary>
        /// Indicates whether we should use Redis server for caching (instead of default in-memory caching)
        /// </summary>
        public bool RedisCachingEnabled { get; set; }
        /// <summary>
        /// Redis connection string. Used when Redis caching is enabled
        /// </summary>
        public string RedisCachingConnectionString { get; set; }



        /// <summary>
        /// Indicates whether we should support previous nopCommerce versions (it can slightly improve performance)
        /// </summary>
        public bool SupportPreviousNopcommerceVersions { get; set; }



        /// <summary>
        /// A value indicating whether the site is run on multiple instances (e.g. web farm, Windows Azure with multiple instances, etc).
        /// Do not enable it if you run on Azure but use one instance only
        /// </summary>
        public bool MultipleInstancesEnabled { get; set; }

        /// <summary>
        /// A value indicating whether the site is run on Windows Azure Web Apps
        /// </summary>
        public bool RunOnAzureWebApps { get; set; }

        /// <summary>
        /// Connection string for Azure BLOB storage
        /// </summary>
        public string AzureBlobStorageConnectionString { get; set; }
        /// <summary>
        /// Container name for Azure BLOB storage
        /// </summary>
        public string AzureBlobStorageContainerName { get; set; }
        /// <summary>
        /// End point for Azure BLOB storage
        /// </summary>
        public string AzureBlobStorageEndPoint { get; set; }


        /// <summary>
        /// A value indicating whether a store owner can install sample data during installation
        /// </summary>
        public bool DisableSampleDataDuringInstallation { get; set; }
        /// <summary>
        /// By default this setting should always be set to "False" (only for advanced users)
        /// </summary>
        public bool UseFastInstallationService { get; set; }
        /// <summary>
        /// A list of plugins ignored during nopCommerce installation
        /// </summary>
        public string PluginsIgnoredDuringInstallation { get; set; }
    }
}
