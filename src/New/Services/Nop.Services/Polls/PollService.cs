using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Polls;
using Nop.Services.Events;

namespace Nop.Services.Polls;

public class PollService : IPollService
{
    private readonly IRepository<Poll> _pollRepository;
    private readonly IRepository<PollAnswer> _pollAnswerRepository;
    private readonly IRepository<PollVotingRecord> _pollVotingRecordRepository;
    private readonly IEventPublisher _eventPublisher;

    public PollService(
        IRepository<Poll> pollRepository,
        IRepository<PollAnswer> pollAnswerRepository,
        IRepository<PollVotingRecord> pollVotingRecordRepository,
        IEventPublisher eventPublisher)
    {
        _pollRepository = pollRepository;
        _pollAnswerRepository = pollAnswerRepository;
        _pollVotingRecordRepository = pollVotingRecordRepository;
        _eventPublisher = eventPublisher;
    }

    public Task<Poll?> GetPollByIdAsync(int pollId)
    {
        return Task.FromResult(pollId == 0 ? null : _pollRepository.GetById(pollId));
    }

    public Task<IPagedList<Poll>> GetPollsAsync(
        int languageId = 0,
        bool loadShownOnHomePageOnly = false,
        string? systemKeyword = null,
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        bool showHidden = false)
    {
        var query = _pollRepository.Table;

        if (!showHidden)
        {
            var utcNow = DateTime.UtcNow;
            query = query.Where(p => p.Published);
            query = query.Where(p => !p.StartDateUtc.HasValue || p.StartDateUtc <= utcNow);
            query = query.Where(p => !p.EndDateUtc.HasValue || p.EndDateUtc >= utcNow);
        }

        if (loadShownOnHomePageOnly)
            query = query.Where(p => p.ShowOnHomePage);

        if (languageId > 0)
            query = query.Where(p => p.LanguageId == languageId);

        if (!string.IsNullOrEmpty(systemKeyword))
            query = query.Where(p => p.SystemKeyword == systemKeyword);

        query = query.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id);

        IPagedList<Poll> result = new PagedList<Poll>(query, pageIndex, pageSize);
        return Task.FromResult(result);
    }

    public async Task DeletePollAsync(Poll poll)
    {
        ArgumentNullException.ThrowIfNull(poll);
        _pollRepository.Delete(poll);
        await _eventPublisher.EntityDeletedAsync(poll);
    }

    public async Task InsertPollAsync(Poll poll)
    {
        ArgumentNullException.ThrowIfNull(poll);
        _pollRepository.Insert(poll);
        await _eventPublisher.EntityInsertedAsync(poll);
    }

    public async Task UpdatePollAsync(Poll poll)
    {
        ArgumentNullException.ThrowIfNull(poll);
        _pollRepository.Update(poll);
        await _eventPublisher.EntityUpdatedAsync(poll);
    }

    public Task<PollAnswer?> GetPollAnswerByIdAsync(int pollAnswerId)
    {
        return Task.FromResult(pollAnswerId == 0 ? null : _pollAnswerRepository.GetById(pollAnswerId));
    }

    public async Task DeletePollAnswerAsync(PollAnswer pollAnswer)
    {
        ArgumentNullException.ThrowIfNull(pollAnswer);
        _pollAnswerRepository.Delete(pollAnswer);
        await _eventPublisher.EntityDeletedAsync(pollAnswer);
    }

    public Task<bool> AlreadyVotedAsync(int pollId, int customerId)
    {
        if (pollId == 0 || customerId == 0)
            return Task.FromResult(false);

        var result = (from pa in _pollAnswerRepository.TableNoTracking
                      join pvr in _pollVotingRecordRepository.TableNoTracking
                          on pa.Id equals pvr.PollAnswerId
                      where pa.PollId == pollId && pvr.CustomerId == customerId
                      select pvr.Id).Any();

        return Task.FromResult(result);
    }
}
