using Nop.Core.Configuration;

namespace Nop.Core.Domain.Cms;

public class WidgetSettings : ISettings
{
    public List<string> ActiveWidgetSystemNames { get; set; } = [];
}
