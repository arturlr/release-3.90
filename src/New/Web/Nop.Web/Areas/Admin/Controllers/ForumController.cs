using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Forums;
using Nop.Services.Forums;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Forums;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ForumController(
    IForumService forumService,
    IDateTimeHelper dateTimeHelper,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Forum groups

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<JsonResult> ForumGroupList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var forumGroups = await forumService.GetAllForumGroupsAsync();

        var gridModel = new DataSourceResult
        {
            Data = forumGroups.Select(fg => new ForumGroupModel
            {
                Id = fg.Id,
                Name = fg.Name,
                DisplayOrder = fg.DisplayOrder,
                CreatedOn = dateTimeHelper.ConvertToUserTime(fg.CreatedOnUtc, DateTimeKind.Utc)
            }),
            Total = forumGroups.Count
        };

        return Json(gridModel);
    }

    public IActionResult CreateForumGroup()
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        return View(new ForumGroupModel { DisplayOrder = 1 });
    }

    [HttpPost]
    public async Task<IActionResult> CreateForumGroup(ForumGroupModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var forumGroup = new ForumGroup
            {
                Name = model.Name,
                DisplayOrder = model.DisplayOrder,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await forumService.InsertForumGroupAsync(forumGroup);

            customerActivityService.InsertActivity("AddNewForumGroup", $"Added a new forum group (ID = {forumGroup.Id})");

            if (continueEditing)
                return RedirectToAction("EditForumGroup", new { id = forumGroup.Id });

            return RedirectToAction("List");
        }

        return View(model);
    }

    public async Task<IActionResult> EditForumGroup(int id)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        var forumGroup = await forumService.GetForumGroupByIdAsync(id);
        if (forumGroup is null)
            return RedirectToAction("List");

        var model = new ForumGroupModel
        {
            Id = forumGroup.Id,
            Name = forumGroup.Name,
            DisplayOrder = forumGroup.DisplayOrder,
            CreatedOn = dateTimeHelper.ConvertToUserTime(forumGroup.CreatedOnUtc, DateTimeKind.Utc)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditForumGroup(ForumGroupModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        var forumGroup = await forumService.GetForumGroupByIdAsync(model.Id);
        if (forumGroup is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            forumGroup.Name = model.Name;
            forumGroup.DisplayOrder = model.DisplayOrder;
            forumGroup.UpdatedOnUtc = DateTime.UtcNow;
            await forumService.UpdateForumGroupAsync(forumGroup);

            customerActivityService.InsertActivity("EditForumGroup", $"Edited a forum group (ID = {forumGroup.Id})");

            if (continueEditing)
                return RedirectToAction("EditForumGroup", new { id = forumGroup.Id });

            return RedirectToAction("List");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteForumGroup(int id)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        var forumGroup = await forumService.GetForumGroupByIdAsync(id);
        if (forumGroup is null)
            return RedirectToAction("List");

        await forumService.DeleteForumGroupAsync(forumGroup);

        customerActivityService.InsertActivity("DeleteForumGroup", $"Deleted a forum group (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Forums

    [HttpPost]
    public async Task<JsonResult> ForumList(int forumGroupId)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var forums = await forumService.GetAllForumsByGroupIdAsync(forumGroupId);

        var gridModel = new DataSourceResult
        {
            Data = forums.Select(f => new ForumModel
            {
                Id = f.Id,
                ForumGroupId = f.ForumGroupId,
                Name = f.Name,
                DisplayOrder = f.DisplayOrder,
                CreatedOn = dateTimeHelper.ConvertToUserTime(f.CreatedOnUtc, DateTimeKind.Utc)
            }),
            Total = forums.Count
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> CreateForum()
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        var model = new ForumModel { DisplayOrder = 1 };
        await PrepareForumGroupDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateForum(ForumModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var forum = new Forum
            {
                ForumGroupId = model.ForumGroupId,
                Name = model.Name,
                Description = model.Description,
                DisplayOrder = model.DisplayOrder,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await forumService.InsertForumAsync(forum);

            customerActivityService.InsertActivity("AddNewForum", $"Added a new forum (ID = {forum.Id})");

            if (continueEditing)
                return RedirectToAction("EditForum", new { id = forum.Id });

            return RedirectToAction("List");
        }

        await PrepareForumGroupDropdownAsync(model);
        return View(model);
    }

    public async Task<IActionResult> EditForum(int id)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        var forum = await forumService.GetForumByIdAsync(id);
        if (forum is null)
            return RedirectToAction("List");

        var model = new ForumModel
        {
            Id = forum.Id,
            ForumGroupId = forum.ForumGroupId,
            Name = forum.Name,
            Description = forum.Description,
            DisplayOrder = forum.DisplayOrder,
            CreatedOn = dateTimeHelper.ConvertToUserTime(forum.CreatedOnUtc, DateTimeKind.Utc)
        };

        await PrepareForumGroupDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditForum(ForumModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        var forum = await forumService.GetForumByIdAsync(model.Id);
        if (forum is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            forum.ForumGroupId = model.ForumGroupId;
            forum.Name = model.Name;
            forum.Description = model.Description;
            forum.DisplayOrder = model.DisplayOrder;
            forum.UpdatedOnUtc = DateTime.UtcNow;
            await forumService.UpdateForumAsync(forum);

            customerActivityService.InsertActivity("EditForum", $"Edited a forum (ID = {forum.Id})");

            if (continueEditing)
                return RedirectToAction("EditForum", new { id = forum.Id });

            return RedirectToAction("List");
        }

        await PrepareForumGroupDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteForum(int id)
    {
        if (!permissionService.Authorize("ManageForums"))
            return Forbid();

        var forum = await forumService.GetForumByIdAsync(id);
        if (forum is null)
            return RedirectToAction("List");

        await forumService.DeleteForumAsync(forum);

        customerActivityService.InsertActivity("DeleteForum", $"Deleted a forum (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Helpers

    private async Task PrepareForumGroupDropdownAsync(ForumModel model)
    {
        foreach (var fg in await forumService.GetAllForumGroupsAsync())
            model.AvailableForumGroups.Add(new SelectListItem { Text = fg.Name, Value = fg.Id.ToString() });
    }

    #endregion
}
