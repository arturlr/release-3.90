namespace Nop.Core.Configuration
{
    /// <summary>
    /// Represents a NopConfig bound from appsettings.json "Nop" section
    /// </summary>
    public partial class NopConfig
    {
        /// <summary>
        /// Indicates whether we should ignore startup tasks
        /// </summary>
        public bool IgnoreStartupTasks { get; set; }

        /// <summary>
        /// Path to database with user agent strings
        /// </summary>
        public string UserAgentStringsPath { get; set; } = string.Empty;

        /// <summary>
        /// Path to database with crawler only user agent strings
        /// </summary>
        public string CrawlerOnlyUserAgentStringsPath { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether we should use Redis server for caching (instead of default in-memory caching)
        /// </summary>
        public bool RedisCachingEnabled { get; set; }

        /// <summary>
        /// Redis connection string. Used when Redis caching is enabled
        /// </summary>
        public string RedisCachingConnectionString { get; set; } = string.Empty;

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
        public string AzureBlobStorageConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Container name for Azure BLOB storage
        /// </summary>
        public string AzureBlobStorageContainerName { get; set; } = string.Empty;

        /// <summary>
        /// End point for Azure BLOB storage
        /// </summary>
        public string AzureBlobStorageEndPoint { get; set; } = string.Empty;

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
        public string PluginsIgnoredDuringInstallation { get; set; } = string.Empty;

        /// <summary>
        /// Custom forwarded HTTP header (e.g. CF-Connecting-IP, X-FORWARDED-PROTO, etc)
        /// </summary>
        public string ForwardedHttpHeader { get; set; } = string.Empty;

        /// <summary>
        /// A value indicating whether to use HTTP_CLUSTER_HTTPS header to determine SSL
        /// </summary>
        public bool UseHttpClusterHttps { get; set; }

        /// <summary>
        /// A value indicating whether to use HTTP_X_FORWARDED_PROTO header to determine SSL
        /// </summary>
        public bool UseHttpXForwardedProto { get; set; }
    }
}
