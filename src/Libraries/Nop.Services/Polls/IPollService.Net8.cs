using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Polls;

namespace Nop.Services.Polls
{
    public interface IPollService
    {
        Task<Poll> GetPollByIdAsync(int pollId);
        Task<IList<Poll>> GetPollsAsync();
        Task InsertPollAsync(Poll poll);
        Task UpdatePollAsync(Poll poll);
        Task DeletePollAsync(Poll poll);
    }
}
