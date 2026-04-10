using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Polls;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Polls;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Polls;

namespace Nop.Web.Controllers;

public class PollController(
    IPollService pollService,
    IWorkContext workContext,
    ICustomerService customerService,
    ILocalizationService localizationService) : BasePublicController
{
    public async Task<IActionResult> PollBlock(string systemKeyword)
    {
        if (string.IsNullOrWhiteSpace(systemKeyword))
            return Content("");

        var languageId = workContext.WorkingLanguage.Id;
        var polls = await pollService.GetPollsAsync(languageId: languageId, systemKeyword: systemKeyword, pageSize: 1);
        var poll = polls.FirstOrDefault();
        if (poll == null)
            return Content("");

        var model = await PreparePollModelAsync(poll, true);
        return PartialView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote(int pollAnswerId)
    {
        var pollAnswer = await pollService.GetPollAnswerByIdAsync(pollAnswerId);
        if (pollAnswer == null)
            return Json(new { error = "No poll answer found with the specified id" });

        var poll = await pollService.GetPollByIdAsync(pollAnswer.PollId);
        if (poll == null || !poll.Published)
            return Json(new { error = "Poll is not available" });

        var customer = workContext.CurrentCustomer;

        if (await IsGuestAsync(customer) && !poll.AllowGuestsToVote)
            return Json(new { error = await localizationService.GetResourceAsync("Polls.OnlyRegisteredUsersVote") });

        if (!await pollService.AlreadyVotedAsync(poll.Id, customer.Id))
        {
            await pollService.InsertPollVotingRecordAsync(new PollVotingRecord
            {
                PollAnswerId = pollAnswer.Id,
                CustomerId = customer.Id,
                CreatedOnUtc = DateTime.UtcNow
            });

            pollAnswer.NumberOfVotes++;
            await pollService.UpdatePollAnswerAsync(pollAnswer);
        }

        var model = await PreparePollModelAsync(poll, true);
        return Json(new { success = true, poll = model });
    }

    public async Task<IActionResult> HomePagePolls()
    {
        var languageId = workContext.WorkingLanguage.Id;
        var polls = await pollService.GetPollsAsync(languageId: languageId, loadShownOnHomePageOnly: true);
        if (!polls.Any())
            return Content("");

        var models = new List<PollModel>();
        foreach (var poll in polls)
            models.Add(await PreparePollModelAsync(poll, true));

        return PartialView(models);
    }

    private async Task<PollModel> PreparePollModelAsync(Poll poll, bool setAlreadyVotedProperty)
    {
        var customerId = workContext.CurrentCustomer.Id;
        var answers = await pollService.GetPollAnswersByPollIdAsync(poll.Id);

        var model = new PollModel
        {
            Id = poll.Id,
            Name = poll.Name,
            AlreadyVoted = setAlreadyVotedProperty && await pollService.AlreadyVotedAsync(poll.Id, customerId)
        };

        foreach (var answer in answers)
            model.TotalVotes += answer.NumberOfVotes;

        foreach (var answer in answers)
        {
            model.Answers.Add(new PollAnswerModel
            {
                Id = answer.Id,
                Name = answer.Name,
                NumberOfVotes = answer.NumberOfVotes,
                PercentOfTotalVotes = model.TotalVotes > 0
                    ? (double)answer.NumberOfVotes / model.TotalVotes * 100
                    : 0
            });
        }

        return model;
    }

    private async Task<bool> IsGuestAsync(Customer customer)
    {
        var guestRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        if (guestRole == null) return false;
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        return roleIds.Contains(guestRole.Id);
    }
}
