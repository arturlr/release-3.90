using Nop.Core;
using Nop.Core.Domain.Polls;

namespace Nop.Services.Polls;

public interface IPollService
{
    Task<Poll?> GetPollByIdAsync(int pollId);

    Task<IPagedList<Poll>> GetPollsAsync(
        int languageId = 0,
        bool loadShownOnHomePageOnly = false,
        string? systemKeyword = null,
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        bool showHidden = false);

    Task DeletePollAsync(Poll poll);

    Task InsertPollAsync(Poll poll);

    Task UpdatePollAsync(Poll poll);

    Task<PollAnswer?> GetPollAnswerByIdAsync(int pollAnswerId);

    Task<IList<PollAnswer>> GetPollAnswersByPollIdAsync(int pollId);

    Task DeletePollAnswerAsync(PollAnswer pollAnswer);

    Task InsertPollAnswerAsync(PollAnswer pollAnswer);

    Task UpdatePollAnswerAsync(PollAnswer pollAnswer);

    Task InsertPollVotingRecordAsync(PollVotingRecord pollVotingRecord);

    Task<bool> AlreadyVotedAsync(int pollId, int customerId);
}
