using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Topics;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Topics;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Topics;

namespace Nop.Web.Controllers;

public class TopicController(
    ITopicService topicService,
    ITopicTemplateService topicTemplateService,
    IWorkContext workContext,
    IStoreContext storeContext,
    IStoreMappingService storeMappingService,
    IAclService aclService,
    IUrlRecordService urlRecordService,
    ILocalizationService localizationService) : BasePublicController
{
    public async Task<IActionResult> TopicDetails(int topicId)
    {
        var topic = await topicService.GetTopicByIdAsync(topicId);
        if (topic == null || !topic.Published)
            return RedirectToAction("Index", "Home");

        if (!await storeMappingService.AuthorizeAsync(topic))
            return RedirectToAction("Index", "Home");

        if (!aclService.Authorize(topic))
            return RedirectToAction("Index", "Home");

        var model = await PrepareTopicModelAsync(topic);
        var templateViewPath = await GetTemplateViewPathAsync(topic.TopicTemplateId);
        return View(templateViewPath, model);
    }

    public async Task<IActionResult> TopicDetailsPopup(string systemName)
    {
        var topic = await topicService.GetTopicBySystemNameAsync(systemName, storeContext.CurrentStore.Id);
        if (topic == null || !topic.Published)
            return RedirectToAction("Index", "Home");

        if (!aclService.Authorize(topic))
            return RedirectToAction("Index", "Home");

        var model = await PrepareTopicModelAsync(topic);
        ViewBag.IsPopup = true;
        var templateViewPath = await GetTemplateViewPathAsync(topic.TopicTemplateId);
        return PartialView(templateViewPath, model);
    }

    public async Task<IActionResult> TopicBlock(string systemName)
    {
        var topic = await topicService.GetTopicBySystemNameAsync(systemName, storeContext.CurrentStore.Id);
        if (topic == null || !topic.Published)
            return Content("");

        if (!aclService.Authorize(topic))
            return Content("");

        var model = await PrepareTopicModelAsync(topic);
        return PartialView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Authenticate(int id, string password)
    {
        var topic = await topicService.GetTopicByIdAsync(id);
        if (topic != null &&
            topic.Published &&
            topic.IsPasswordProtected &&
            await storeMappingService.AuthorizeAsync(topic) &&
            aclService.Authorize(topic))
        {
            if (topic.Password != null && topic.Password.Equals(password))
            {
                return Json(new
                {
                    Authenticated = true,
                    Title = topic.Title ?? string.Empty,
                    Body = topic.Body ?? string.Empty,
                    Error = string.Empty
                });
            }

            return Json(new
            {
                Authenticated = false,
                Title = string.Empty,
                Body = string.Empty,
                Error = await localizationService.GetResourceAsync("Topic.WrongPassword")
            });
        }

        return Json(new
        {
            Authenticated = false,
            Title = string.Empty,
            Body = string.Empty,
            Error = await localizationService.GetResourceAsync("Topic.WrongPassword")
        });
    }

    private async Task<TopicModel> PrepareTopicModelAsync(Topic topic)
    {
        var languageId = workContext.WorkingLanguage.Id;
        var seName = await urlRecordService.GetActiveSlugAsync(topic.Id, "Topic", languageId);
        if (string.IsNullOrEmpty(seName))
            seName = await urlRecordService.GetActiveSlugAsync(topic.Id, "Topic", 0);

        return new TopicModel
        {
            Id = topic.Id,
            SystemName = topic.SystemName,
            IncludeInSitemap = topic.IncludeInSitemap,
            IsPasswordProtected = topic.IsPasswordProtected,
            Title = topic.IsPasswordProtected ? string.Empty : topic.Title,
            Body = topic.IsPasswordProtected ? string.Empty : topic.Body,
            MetaKeywords = topic.MetaKeywords,
            MetaDescription = topic.MetaDescription,
            MetaTitle = topic.MetaTitle,
            SeName = seName,
            TopicTemplateId = topic.TopicTemplateId
        };
    }

    private async Task<string> GetTemplateViewPathAsync(int topicTemplateId)
    {
        var template = await topicTemplateService.GetTopicTemplateByIdAsync(topicTemplateId);
        if (template == null)
        {
            var all = await topicTemplateService.GetAllTopicTemplatesAsync();
            template = all.FirstOrDefault();
        }

        return template?.ViewPath ?? "TopicDetails";
    }
}
