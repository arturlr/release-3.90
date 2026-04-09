using Nop.Core.Domain.Topics;

namespace Nop.Services.Topics;

public interface ITopicService
{
    Task<Topic?> GetTopicByIdAsync(int topicId);

    Task<Topic?> GetTopicBySystemNameAsync(string systemName, int storeId = 0);

    Task<IList<Topic>> GetAllTopicsAsync(int storeId, bool ignoreAcl = false, bool showHidden = false);

    Task InsertTopicAsync(Topic topic);

    Task UpdateTopicAsync(Topic topic);

    Task DeleteTopicAsync(Topic topic);
}
