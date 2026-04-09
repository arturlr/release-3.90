using Nop.Core.Data;
using Nop.Core.Domain.Topics;
using Nop.Services.Events;

namespace Nop.Services.Topics;

public class TopicTemplateService : ITopicTemplateService
{
    private readonly IRepository<TopicTemplate> _topicTemplateRepository;
    private readonly IEventPublisher _eventPublisher;

    public TopicTemplateService(
        IRepository<TopicTemplate> topicTemplateRepository,
        IEventPublisher eventPublisher)
    {
        _topicTemplateRepository = topicTemplateRepository;
        _eventPublisher = eventPublisher;
    }

    public Task<TopicTemplate?> GetTopicTemplateByIdAsync(int topicTemplateId)
    {
        return Task.FromResult(topicTemplateId == 0 ? null : _topicTemplateRepository.GetById(topicTemplateId));
    }

    public Task<IList<TopicTemplate>> GetAllTopicTemplatesAsync()
    {
        IList<TopicTemplate> templates = _topicTemplateRepository.TableNoTracking
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Id)
            .ToList();

        return Task.FromResult(templates);
    }

    public async Task InsertTopicTemplateAsync(TopicTemplate topicTemplate)
    {
        ArgumentNullException.ThrowIfNull(topicTemplate);
        _topicTemplateRepository.Insert(topicTemplate);
        await _eventPublisher.EntityInsertedAsync(topicTemplate);
    }

    public async Task UpdateTopicTemplateAsync(TopicTemplate topicTemplate)
    {
        ArgumentNullException.ThrowIfNull(topicTemplate);
        _topicTemplateRepository.Update(topicTemplate);
        await _eventPublisher.EntityUpdatedAsync(topicTemplate);
    }

    public async Task DeleteTopicTemplateAsync(TopicTemplate topicTemplate)
    {
        ArgumentNullException.ThrowIfNull(topicTemplate);
        _topicTemplateRepository.Delete(topicTemplate);
        await _eventPublisher.EntityDeletedAsync(topicTemplate);
    }
}
