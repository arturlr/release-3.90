using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Orders;

public class CheckoutAttributeModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? TextPrompt { get; set; }
    public bool IsRequired { get; set; }
    public bool ShippableProductRequired { get; set; }
    public bool IsTaxExempt { get; set; }
    public int TaxCategoryId { get; set; }
    public int AttributeControlTypeId { get; set; }
    public int DisplayOrder { get; set; }

    // validation
    public int? ValidationMinLength { get; set; }
    public int? ValidationMaxLength { get; set; }
    public string? ValidationFileAllowedExtensions { get; set; }
    public int? ValidationFileMaximumSize { get; set; }
    public string? DefaultValue { get; set; }

    // dropdowns
    public List<TaxCategoryItem> AvailableTaxCategories { get; set; } = [];
    public List<ControlTypeItem> AvailableControlTypes { get; set; } = [];

    // grid display
    public string? AttributeControlTypeName { get; set; }
}

public class CheckoutAttributeValueModel : BaseNopEntityModel
{
    public int CheckoutAttributeId { get; set; }
    public string? Name { get; set; }
    public string? ColorSquaresRgb { get; set; }
    public decimal PriceAdjustment { get; set; }
    public decimal WeightAdjustment { get; set; }
    public bool IsPreSelected { get; set; }
    public int DisplayOrder { get; set; }
}

public class TaxCategoryItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class ControlTypeItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
