using Nop.Core.Configuration;

namespace Nop.Core.Domain;

public class StoreInformationSettings : ISettings
{
    public bool HidePoweredByNopCommerce { get; set; }
    public bool StoreClosed { get; set; }
    public int LogoPictureId { get; set; }
    public string? DefaultStoreTheme { get; set; }
    public bool AllowCustomerToSelectTheme { get; set; }
    public bool DisplayMiniProfilerInPublicStore { get; set; }
    public bool DisplayMiniProfilerForAdminOnly { get; set; }
    public bool DisplayEuCookieLawWarning { get; set; }
    public string? FacebookLink { get; set; }
    public string? TwitterLink { get; set; }
    public string? YoutubeLink { get; set; }
    public string? GooglePlusLink { get; set; }
}
