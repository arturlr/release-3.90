using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Events;

namespace Nop.Services.Catalog;

public class ManufacturerTemplateService : IManufacturerTemplateService
{
    private readonly IRepository<ManufacturerTemplate> _manufacturerTemplateRepository;
    private readonly IEventPublisher _eventPublisher;

    public ManufacturerTemplateService(IRepository<ManufacturerTemplate> manufacturerTemplateRepository, IEventPublisher eventPublisher)
    {
        _manufacturerTemplateRepository = manufacturerTemplateRepository;
        _eventPublisher = eventPublisher;
    }

    public Task<ManufacturerTemplate?> GetManufacturerTemplateByIdAsync(int manufacturerTemplateId) =>
        Task.FromResult(manufacturerTemplateId == 0 ? null : _manufacturerTemplateRepository.GetById(manufacturerTemplateId));

    public Task<IList<ManufacturerTemplate>> GetAllManufacturerTemplatesAsync()
    {
        IList<ManufacturerTemplate> templates = _manufacturerTemplateRepository.TableNoTracking
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id).ToList();
        return Task.FromResult(templates);
    }

    public async Task InsertManufacturerTemplateAsync(ManufacturerTemplate manufacturerTemplate)
    {
        ArgumentNullException.ThrowIfNull(manufacturerTemplate);
        _manufacturerTemplateRepository.Insert(manufacturerTemplate);
        await _eventPublisher.EntityInsertedAsync(manufacturerTemplate);
    }

    public async Task UpdateManufacturerTemplateAsync(ManufacturerTemplate manufacturerTemplate)
    {
        ArgumentNullException.ThrowIfNull(manufacturerTemplate);
        _manufacturerTemplateRepository.Update(manufacturerTemplate);
        await _eventPublisher.EntityUpdatedAsync(manufacturerTemplate);
    }

    public async Task DeleteManufacturerTemplateAsync(ManufacturerTemplate manufacturerTemplate)
    {
        ArgumentNullException.ThrowIfNull(manufacturerTemplate);
        _manufacturerTemplateRepository.Delete(manufacturerTemplate);
        await _eventPublisher.EntityDeletedAsync(manufacturerTemplate);
    }
}
