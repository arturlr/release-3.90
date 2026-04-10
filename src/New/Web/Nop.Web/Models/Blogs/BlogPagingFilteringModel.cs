using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Blogs;

public class BlogPagingFilteringModel : BasePageableModel
{
    public string? Month { get; set; }
    public string? Tag { get; set; }

    public DateTime? GetParsedMonth()
    {
        if (string.IsNullOrEmpty(Month))
            return null;

        var parts = Month.Split('-');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var year) &&
            int.TryParse(parts[1], out var month))
            return new DateTime(year, month, 1);

        return null;
    }

    public DateTime? GetFromMonth() => GetParsedMonth();

    public DateTime? GetToMonth()
    {
        var parsed = GetParsedMonth();
        return parsed?.AddMonths(1).AddSeconds(-1);
    }
}
