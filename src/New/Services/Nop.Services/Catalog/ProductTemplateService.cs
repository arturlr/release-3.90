using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Events;

namespace Nop.Services.Catalog;

public class ProductTemplateService : IProductTemplateService
{
    private readonly IRepository<ProductTemplate> _productTemplateRepository;
    private readonly IEventPublisher _eventPublisher;

    public ProductTemplateService(IRepository<ProductTemplate> productTemplateRepository, IEventPublisher eventPublisher)
    {
        _productTemplateRepository = productTemplateRepository;
        _eventPublisher = eventPublisher;
    }

    public Task<ProductTemplate?> GetProductTemplateByIdAsync(int productTemplateId) =>
        Task.FromResult(productTemplateId == 0 ? null : _productTemplateRepository.GetById(productTemplateId));

    public Task<IList<ProductTemplate>> GetAllProductTemplatesAsync()
    {
        IList<ProductTemplate> templates = _productTemplateRepository.TableNoTracking
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id).ToList();
        return Task.FromResult(templates);
    }

    public async Task InsertProductTemplateAsync(ProductTemplate productTemplate)
    {
        ArgumentNullException.ThrowIfNull(productTemplate);
        _productTemplateRepository.Insert(productTemplate);
        await _eventPublisher.EntityInsertedAsync(productTemplate);
    }

    public async Task UpdateProductTemplateAsync(ProductTemplate productTemplate)
    {
        ArgumentNullException.ThrowIfNull(productTemplate);
        _productTemplateRepository.Update(productTemplate);
        await _eventPublisher.EntityUpdatedAsync(productTemplate);
    }

    public async Task DeleteProductTemplateAsync(ProductTemplate productTemplate)
    {
        ArgumentNullException.ThrowIfNull(productTemplate);
        _productTemplateRepository.Delete(productTemplate);
        await _eventPublisher.EntityDeletedAsync(productTemplate);
    }
}
