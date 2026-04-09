using Nop.Core.Configuration;

namespace Nop.Core.Domain.Common;

public class AdminAreaSettings : ISettings
{
    public int DefaultGridPageSize { get; set; }
    public int PopupGridPageSize { get; set; }
    public string? GridPageSizes { get; set; }
    public string? RichEditorAdditionalSettings { get; set; }
    public bool RichEditorAllowJavaScript { get; set; }
    public bool UseRichEditorInMessageTemplates { get; set; }
    public bool HideAdvertisementsOnAdminArea { get; set; }
    public string? LastNewsTitleAdminArea { get; set; }
    public bool UseIsoDateTimeConverterInJson { get; set; }
}
