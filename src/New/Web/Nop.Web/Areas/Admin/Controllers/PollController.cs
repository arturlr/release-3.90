using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Polls;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Polls;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Polls;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class PollController(
    IPollService pollService,
    ILanguageService languageService,
    IDateTimeHelper dateTimeHelper,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Polls

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<JsonResult> PollList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var polls = await pollService.GetPollsAsync(
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true);

        var languages = await languageService.GetAllLanguagesAsync(showHidden: true);
        var languageLookup = languages.ToDictionary(l => l.Id, l => l.Name);

        var gridModel = new DataSourceResult
        {
            Data = polls.Select(p => new PollGridModel
            {
                Id = p.Id,
                Name = p.Name,
                LanguageName = languageLookup.GetValueOrDefault(p.LanguageId, ""),
                DisplayOrder = p.DisplayOrder,
                Published = p.Published,
                ShowOnHomePage = p.ShowOnHomePage,
                StartDate = p.StartDateUtc.HasValue ? dateTimeHelper.ConvertToUserTime(p.StartDateUtc.Value, DateTimeKind.Utc) : null,
                EndDate = p.EndDateUtc.HasValue ? dateTimeHelper.ConvertToUserTime(p.EndDateUtc.Value, DateTimeKind.Utc) : null
            }),
            Total = polls.TotalCount
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Forbid();

        var model = new PollModel { Published = true, ShowOnHomePage = true };
        await PrepareLanguageDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PollModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var poll = new Poll
            {
                LanguageId = model.LanguageId,
                Name = model.Name ?? string.Empty,
                SystemKeyword = model.SystemKeyword,
                Published = model.Published,
                ShowOnHomePage = model.ShowOnHomePage,
                AllowGuestsToVote = model.AllowGuestsToVote,
                DisplayOrder = model.DisplayOrder,
                StartDateUtc = model.StartDate,
                EndDateUtc = model.EndDate
            };
            await pollService.InsertPollAsync(poll);

            customerActivityService.InsertActivity("AddNewPoll", $"Added a new poll (ID = {poll.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = poll.Id });

            return RedirectToAction("List");
        }

        await PrepareLanguageDropdownAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Forbid();

        var poll = await pollService.GetPollByIdAsync(id);
        if (poll is null)
            return RedirectToAction("List");

        var model = new PollModel
        {
            Id = poll.Id,
            LanguageId = poll.LanguageId,
            Name = poll.Name,
            SystemKeyword = poll.SystemKeyword,
            Published = poll.Published,
            ShowOnHomePage = poll.ShowOnHomePage,
            AllowGuestsToVote = poll.AllowGuestsToVote,
            DisplayOrder = poll.DisplayOrder,
            StartDate = poll.StartDateUtc,
            EndDate = poll.EndDateUtc
        };

        await PrepareLanguageDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(PollModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Forbid();

        var poll = await pollService.GetPollByIdAsync(model.Id);
        if (poll is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            poll.LanguageId = model.LanguageId;
            poll.Name = model.Name ?? string.Empty;
            poll.SystemKeyword = model.SystemKeyword;
            poll.Published = model.Published;
            poll.ShowOnHomePage = model.ShowOnHomePage;
            poll.AllowGuestsToVote = model.AllowGuestsToVote;
            poll.DisplayOrder = model.DisplayOrder;
            poll.StartDateUtc = model.StartDate;
            poll.EndDateUtc = model.EndDate;
            await pollService.UpdatePollAsync(poll);

            customerActivityService.InsertActivity("EditPoll", $"Edited a poll (ID = {poll.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = poll.Id });

            return RedirectToAction("List");
        }

        await PrepareLanguageDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Forbid();

        var poll = await pollService.GetPollByIdAsync(id);
        if (poll is null)
            return RedirectToAction("List");

        await pollService.DeletePollAsync(poll);

        customerActivityService.InsertActivity("DeletePoll", $"Deleted a poll (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Poll answers

    [HttpPost]
    public async Task<JsonResult> PollAnswerList(int pollId)
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var answers = await pollService.GetPollAnswersByPollIdAsync(pollId);

        var gridModel = new DataSourceResult
        {
            Data = answers.Select(a => new PollAnswerModel
            {
                Id = a.Id,
                PollId = a.PollId,
                Name = a.Name,
                NumberOfVotes = a.NumberOfVotes,
                DisplayOrder = a.DisplayOrder
            }),
            Total = answers.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<JsonResult> PollAnswerAdd(int pollId, PollAnswerModel model)
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        await pollService.InsertPollAnswerAsync(new PollAnswer
        {
            PollId = pollId,
            Name = model.Name ?? string.Empty,
            DisplayOrder = model.DisplayOrder
        });

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> PollAnswerUpdate(PollAnswerModel model)
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var pollAnswer = await pollService.GetPollAnswerByIdAsync(model.Id);
        if (pollAnswer is null)
            return Json(new DataSourceResult { Errors = "Poll answer not found" });

        pollAnswer.Name = model.Name ?? string.Empty;
        pollAnswer.DisplayOrder = model.DisplayOrder;
        await pollService.UpdatePollAnswerAsync(pollAnswer);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> PollAnswerDelete(int id)
    {
        if (!permissionService.Authorize("ManagePolls"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var pollAnswer = await pollService.GetPollAnswerByIdAsync(id);
        if (pollAnswer is null)
            return Json(new DataSourceResult { Errors = "Poll answer not found" });

        await pollService.DeletePollAnswerAsync(pollAnswer);

        return Json(new { });
    }

    #endregion

    #region Helpers

    private async Task PrepareLanguageDropdownAsync(PollModel model)
    {
        foreach (var lang in await languageService.GetAllLanguagesAsync(showHidden: true))
            model.AvailableLanguages.Add(new SelectListItem { Text = lang.Name, Value = lang.Id.ToString() });
    }

    #endregion
}
