using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Polls;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class PollController : BasePublicController
    {
        private readonly IPollService _pollService;

        public PollController(
            IWorkContext workContext,
            IPollService pollService) : base(workContext)
        {
            _pollService = pollService;
        }

        // POST: /Poll/Vote
        [HttpPost]
        public async Task<IActionResult> Vote(int pollAnswerId)
        {
            // TODO: Record vote
            return Json(new { success = true, message = "Vote recorded" });
        }

        // GET: /Poll/GetPoll/5
        public async Task<IActionResult> GetPoll(int pollId)
        {
            var poll = await _pollService.GetPollByIdAsync(pollId);
            if (poll == null || !poll.Published)
                return NotFound();

            return Content($"Poll: {poll.Name}");
        }
    }
}
