using Nop.Core.Domain.Topics;

namespace Nop.Services.Topics;

public interface ITopicTemplateService
{
    Task<TopicTemplate?> GetTopicTemplateByIdAsync(int topicTemplateId);

    Task<IList<TopicTemplate>> GetAllTopicTemplatesAsync();

    Task InsertTopicTemplateAsync(TopicTemplate topicTemplate);

    Task UpdateTopicTemplateAsync(TopicTemplate topicTemplate);

    Task DeleteTopicTemplateAsync(TopicTemplate topicTemplate);
}
