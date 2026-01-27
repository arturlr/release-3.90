using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Polls;
using Nop.Data;

namespace Nop.Services.Polls
{
    public class PollService : IPollService
    {
        private readonly IRepository<Poll> _pollRepository;

        public PollService(IRepository<Poll> pollRepository)
        {
            _pollRepository = pollRepository;
        }

        public virtual async Task<Poll> GetPollByIdAsync(int pollId)
        {
            if (pollId == 0)
                return null;

            return await _pollRepository.GetByIdAsync(pollId);
        }

        public virtual async Task<IList<Poll>> GetPollsAsync()
        {
            var query = _pollRepository.Table
                .Where(p => p.Published)
                .OrderBy(p => p.DisplayOrder);

            return await query.ToListAsync();
        }

        public virtual async Task InsertPollAsync(Poll poll)
        {
            if (poll == null)
                throw new ArgumentNullException(nameof(poll));

            await _pollRepository.InsertAsync(poll);
        }

        public virtual async Task UpdatePollAsync(Poll poll)
        {
            if (poll == null)
                throw new ArgumentNullException(nameof(poll));

            await _pollRepository.UpdateAsync(poll);
        }

        public virtual async Task DeletePollAsync(Poll poll)
        {
            if (poll == null)
                throw new ArgumentNullException(nameof(poll));

            await _pollRepository.DeleteAsync(poll);
        }
    }
}
