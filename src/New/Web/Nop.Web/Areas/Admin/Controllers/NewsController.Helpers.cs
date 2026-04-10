using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.News;
using Nop.Web.Areas.Admin.Models.News;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class NewsController
{
    private async Task PrepareNewsItemModelDropdownsAsync(NewsItemModel model)
    {
        foreach (var lang in await languageService.GetAllLanguagesAsync(showHidden: true))
            model.AvailableLanguages.Add(new SelectListItem { Text = lang.Name, Value = lang.Id.ToString() });
    }

    private static NewsItem MapModelToEntity(NewsItemModel model, NewsItem entity)
    {
        entity.LanguageId = model.LanguageId;
        entity.Title = model.Title;
        entity.Short = model.Short;
        entity.Full = model.Full;
        entity.Published = model.Published;
        entity.AllowComments = model.AllowComments;
        entity.StartDateUtc = model.StartDate;
        entity.EndDateUtc = model.EndDate;
        entity.MetaKeywords = model.MetaKeywords;
        entity.MetaDescription = model.MetaDescription;
        entity.MetaTitle = model.MetaTitle;
        return entity;
    }

    private static NewsItemModel MapEntityToModel(NewsItem entity)
    {
        return new NewsItemModel
        {
            Id = entity.Id,
            LanguageId = entity.LanguageId,
            Title = entity.Title,
            Short = entity.Short,
            Full = entity.Full,
            Published = entity.Published,
            AllowComments = entity.AllowComments,
            StartDate = entity.StartDateUtc,
            EndDate = entity.EndDateUtc,
            MetaKeywords = entity.MetaKeywords,
            MetaDescription = entity.MetaDescription,
            MetaTitle = entity.MetaTitle
        };
    }
}
