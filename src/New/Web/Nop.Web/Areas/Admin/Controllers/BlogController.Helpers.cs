using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Blogs;
using Nop.Web.Areas.Admin.Models.Blogs;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class BlogController
{
    private async Task PrepareBlogPostModelDropdownsAsync(BlogPostModel model)
    {
        foreach (var lang in await languageService.GetAllLanguagesAsync(showHidden: true))
            model.AvailableLanguages.Add(new SelectListItem { Text = lang.Name, Value = lang.Id.ToString() });
    }

    private static BlogPost MapModelToEntity(BlogPostModel model, BlogPost entity)
    {
        entity.LanguageId = model.LanguageId;
        entity.Title = model.Title;
        entity.Body = model.Body;
        entity.BodyOverview = model.BodyOverview;
        entity.AllowComments = model.AllowComments;
        entity.Tags = model.Tags;
        entity.StartDateUtc = model.StartDate;
        entity.EndDateUtc = model.EndDate;
        entity.MetaKeywords = model.MetaKeywords;
        entity.MetaDescription = model.MetaDescription;
        entity.MetaTitle = model.MetaTitle;
        return entity;
    }

    private static BlogPostModel MapEntityToModel(BlogPost entity)
    {
        return new BlogPostModel
        {
            Id = entity.Id,
            LanguageId = entity.LanguageId,
            Title = entity.Title,
            Body = entity.Body,
            BodyOverview = entity.BodyOverview,
            AllowComments = entity.AllowComments,
            Tags = entity.Tags,
            StartDate = entity.StartDateUtc,
            EndDate = entity.EndDateUtc,
            MetaKeywords = entity.MetaKeywords,
            MetaDescription = entity.MetaDescription,
            MetaTitle = entity.MetaTitle
        };
    }
}
