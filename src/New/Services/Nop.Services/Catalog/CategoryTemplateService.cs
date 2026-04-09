using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Events;

namespace Nop.Services.Catalog;

public class CategoryTemplateService : ICategoryTemplateService
{
    private readonly IRepository<CategoryTemplate> _categoryTemplateRepository;
    private readonly IEventPublisher _eventPublisher;

    public CategoryTemplateService(IRepository<CategoryTemplate> categoryTemplateRepository, IEventPublisher eventPublisher)
    {
        _categoryTemplateRepository = categoryTemplateRepository;
        _eventPublisher = eventPublisher;
    }

    public Task<CategoryTemplate?> GetCategoryTemplateByIdAsync(int categoryTemplateId) =>
        Task.FromResult(categoryTemplateId == 0 ? null : _categoryTemplateRepository.GetById(categoryTemplateId));

    public Task<IList<CategoryTemplate>> GetAllCategoryTemplatesAsync()
    {
        IList<CategoryTemplate> templates = _categoryTemplateRepository.TableNoTracking
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id).ToList();
        return Task.FromResult(templates);
    }

    public async Task InsertCategoryTemplateAsync(CategoryTemplate categoryTemplate)
    {
        ArgumentNullException.ThrowIfNull(categoryTemplate);
        _categoryTemplateRepository.Insert(categoryTemplate);
        await _eventPublisher.EntityInsertedAsync(categoryTemplate);
    }

    public async Task UpdateCategoryTemplateAsync(CategoryTemplate categoryTemplate)
    {
        ArgumentNullException.ThrowIfNull(categoryTemplate);
        _categoryTemplateRepository.Update(categoryTemplate);
        await _eventPublisher.EntityUpdatedAsync(categoryTemplate);
    }

    public async Task DeleteCategoryTemplateAsync(CategoryTemplate categoryTemplate)
    {
        ArgumentNullException.ThrowIfNull(categoryTemplate);
        _categoryTemplateRepository.Delete(categoryTemplate);
        await _eventPublisher.EntityDeletedAsync(categoryTemplate);
    }
}
