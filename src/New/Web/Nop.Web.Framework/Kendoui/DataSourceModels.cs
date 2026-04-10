using System.Collections;

namespace Nop.Web.Framework.Kendoui;

/// <summary>
/// Kendo UI DataSource request model for admin grids.
/// </summary>
public class DataSourceRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Kendo UI DataSource result model for admin grids.
/// </summary>
public class DataSourceResult
{
    public IEnumerable? Data { get; set; }
    public int Total { get; set; }
    public object? Errors { get; set; }
    public object? ExtraData { get; set; }
}
