using Nop.Core.Configuration;

namespace Nop.Core.Domain.Common;

public class DisplayDefaultMenuItemSettings : ISettings
{
    public bool DisplayHomePageMenuItem { get; set; }
    public bool DisplayNewProductsMenuItem { get; set; }
    public bool DisplayProductSearchMenuItem { get; set; }
    public bool DisplayCustomerInfoMenuItem { get; set; }
    public bool DisplayBlogMenuItem { get; set; }
    public bool DisplayForumsMenuItem { get; set; }
    public bool DisplayContactUsMenuItem { get; set; }
}
