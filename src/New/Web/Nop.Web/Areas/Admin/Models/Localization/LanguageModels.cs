using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Localization;

public class LanguageModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? LanguageCulture { get; set; }
    public string? UniqueSeoCode { get; set; }
    public string? FlagImageFileName { get; set; }
    public bool Rtl { get; set; }
    public int DefaultCurrencyId { get; set; }
    public IList<SelectListItem> AvailableCurrencies { get; set; } = [];
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }

    // resource search
    public string? SearchResourceName { get; set; }
    public string? SearchResourceValue { get; set; }
}

public class LanguageGridModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? LanguageCulture { get; set; }
    public string? UniqueSeoCode { get; set; }
    public bool Published { get; set; }
    public int DisplayOrder { get; set; }
}

public class LanguageResourceModel : BaseNopEntityModel
{
    public int LanguageId { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
}
