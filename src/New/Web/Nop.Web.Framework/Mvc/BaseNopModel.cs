namespace Nop.Web.Framework.Mvc;

/// <summary>
/// Base nopCommerce model
/// </summary>
public partial class BaseNopModel
{
    public Dictionary<string, object> CustomProperties { get; set; } = [];
}

/// <summary>
/// Base nopCommerce entity model
/// </summary>
public partial class BaseNopEntityModel : BaseNopModel
{
    public int Id { get; set; }
}

/// <summary>
/// Paging model for admin grids
/// </summary>
public partial class BasePageableModel : BaseNopModel
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int AvailablePageSizes { get; set; }
}
