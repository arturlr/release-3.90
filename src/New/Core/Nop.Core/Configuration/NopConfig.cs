namespace Nop.Core.Configuration;

/// <summary>
/// Application-level configuration. Bound from appsettings.json "Nop" section via IOptions&lt;NopConfig&gt;.
/// Replaces legacy XML IConfigurationSectionHandler.
/// </summary>
public class NopConfig
{
    public bool IgnoreStartupTasks { get; set; }
    public string? UserAgentStringsPath { get; set; }
    public string? CrawlerOnlyUserAgentStringsPath { get; set; }

    // Redis
    public bool RedisCachingEnabled { get; set; }
    public string? RedisCachingConnectionString { get; set; }

    // Web farms
    public bool MultipleInstancesEnabled { get; set; }
    public bool RunOnAzureWebApps { get; set; }

    // Azure Blob Storage
    public string? AzureBlobStorageConnectionString { get; set; }
    public string? AzureBlobStorageContainerName { get; set; }
    public string? AzureBlobStorageEndPoint { get; set; }

    // Installation
    public bool DisableSampleDataDuringInstallation { get; set; }
    public bool UseFastInstallationService { get; set; }
    public string? PluginsIgnoredDuringInstallation { get; set; }
}
