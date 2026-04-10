using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Topics;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Topics;
using Nop.Web.Areas.Admin.Models.Topics;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class TopicController(
    ITopicService topicService,
    ITopicTemplateService topicTemplateService,
    IUrlRecordService urlRecordService,
    IStoreService storeService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    SeoSettings seoSettings) : BaseAdminController
{
    #region List

    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageTopics"))
            return Forbid();

        var model = new TopicListModel();
        model.AvailableStores.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var s in await storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> TopicList(TopicListModel model)
    {
        if (!permissionService.Authorize("ManageTopics"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var topics = await topicService.GetAllTopicsAsync(model.SearchStoreId, showHidden: true);

        var gridModel = new DataSourceResult
        {
            Data = topics.Select(t => new TopicGridModel
            {
                Id = t.Id,
                SystemName = t.SystemName,
                Title = t.Title,
                Published = t.Published,
                IsPasswordProtected = t.IsPasswordProtected,
                IncludeInTopMenu = t.IncludeInTopMenu,
                DisplayOrder = t.DisplayOrder
            }),
            Total = topics.Count
        };

        return Json(gridModel);
    }

    #endregion

    #region Create / Edit / Delete

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageTopics"))
            return Forbid();

        var model = new TopicModel { DisplayOrder = 1, Published = true };
        await PrepareTopicTemplateDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TopicModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageTopics"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var topic = MapModelToEntity(model, new Topic());
            if (!topic.IsPasswordProtected)
                topic.Password = null;

            await topicService.InsertTopicAsync(topic);

            var seName = await topic.ValidateSeNameAsync(
                model.SeName, topic.Title ?? topic.SystemName ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(topic, seName, 0);

            customerActivityService.InsertActivity("AddNewTopic", $"Added a new topic (ID = {topic.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = topic.Id });

            return RedirectToAction("List");
        }

        await PrepareTopicTemplateDropdownAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageTopics"))
            return Forbid();

        var topic = await topicService.GetTopicByIdAsync(id);
        if (topic is null)
            return RedirectToAction("List");

        var model = MapEntityToModel(topic);
        model.SeName = await urlRecordService.GetActiveSlugAsync(topic.Id, "Topic", 0);
        await PrepareTopicTemplateDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TopicModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageTopics"))
            return Forbid();

        var topic = await topicService.GetTopicByIdAsync(model.Id);
        if (topic is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            topic = MapModelToEntity(model, topic);
            if (!topic.IsPasswordProtected)
                topic.Password = null;

            await topicService.UpdateTopicAsync(topic);

            var seName = await topic.ValidateSeNameAsync(
                model.SeName, topic.Title ?? topic.SystemName ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(topic, seName, 0);

            customerActivityService.InsertActivity("EditTopic", $"Edited a topic (ID = {topic.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = topic.Id });

            return RedirectToAction("List");
        }

        await PrepareTopicTemplateDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageTopics"))
            return Forbid();

        var topic = await topicService.GetTopicByIdAsync(id);
        if (topic is null)
            return RedirectToAction("List");

        await topicService.DeleteTopicAsync(topic);

        customerActivityService.InsertActivity("DeleteTopic", $"Deleted a topic (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Helpers

    private async Task PrepareTopicTemplateDropdownAsync(TopicModel model)
    {
        foreach (var template in await topicTemplateService.GetAllTopicTemplatesAsync())
            model.AvailableTopicTemplates.Add(new SelectListItem { Text = template.Name, Value = template.Id.ToString() });
    }

    private static TopicModel MapEntityToModel(Topic topic) => new()
    {
        Id = topic.Id,
        SystemName = topic.SystemName,
        IncludeInSitemap = topic.IncludeInSitemap,
        IncludeInTopMenu = topic.IncludeInTopMenu,
        IncludeInFooterColumn1 = topic.IncludeInFooterColumn1,
        IncludeInFooterColumn2 = topic.IncludeInFooterColumn2,
        IncludeInFooterColumn3 = topic.IncludeInFooterColumn3,
        DisplayOrder = topic.DisplayOrder,
        AccessibleWhenStoreClosed = topic.AccessibleWhenStoreClosed,
        IsPasswordProtected = topic.IsPasswordProtected,
        Password = topic.Password,
        Title = topic.Title,
        Body = topic.Body,
        Published = topic.Published,
        TopicTemplateId = topic.TopicTemplateId,
        MetaKeywords = topic.MetaKeywords,
        MetaDescription = topic.MetaDescription,
        MetaTitle = topic.MetaTitle
    };

    private static Topic MapModelToEntity(TopicModel model, Topic topic)
    {
        topic.SystemName = model.SystemName;
        topic.IncludeInSitemap = model.IncludeInSitemap;
        topic.IncludeInTopMenu = model.IncludeInTopMenu;
        topic.IncludeInFooterColumn1 = model.IncludeInFooterColumn1;
        topic.IncludeInFooterColumn2 = model.IncludeInFooterColumn2;
        topic.IncludeInFooterColumn3 = model.IncludeInFooterColumn3;
        topic.DisplayOrder = model.DisplayOrder;
        topic.AccessibleWhenStoreClosed = model.AccessibleWhenStoreClosed;
        topic.IsPasswordProtected = model.IsPasswordProtected;
        topic.Password = model.Password;
        topic.Title = model.Title;
        topic.Body = model.Body;
        topic.Published = model.Published;
        topic.TopicTemplateId = model.TopicTemplateId;
        topic.MetaKeywords = model.MetaKeywords;
        topic.MetaDescription = model.MetaDescription;
        topic.MetaTitle = model.MetaTitle;
        return topic;
    }

    #endregion
}
