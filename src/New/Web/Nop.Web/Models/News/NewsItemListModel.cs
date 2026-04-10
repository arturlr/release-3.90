using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.News;

public class NewsItemListModel : BaseNopModel
{
    public int WorkingLanguageId { get; set; }
    public NewsPagingFilteringModel PagingFilteringContext { get; set; } = new();
    public IList<NewsItemModel> NewsItems { get; set; } = [];
}
