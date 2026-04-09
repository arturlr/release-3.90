using Nop.Core.Data;
using Nop.Core.Domain.Shipping;
using Nop.Services.Events;

namespace Nop.Services.Shipping;

public class DateRangeService : IDateRangeService
{
    private readonly IRepository<DeliveryDate> _deliveryDateRepository;
    private readonly IRepository<ProductAvailabilityRange> _productAvailabilityRangeRepository;
    private readonly IEventPublisher _eventPublisher;

    public DateRangeService(
        IRepository<DeliveryDate> deliveryDateRepository,
        IRepository<ProductAvailabilityRange> productAvailabilityRangeRepository,
        IEventPublisher eventPublisher)
    {
        _deliveryDateRepository = deliveryDateRepository;
        _productAvailabilityRangeRepository = productAvailabilityRangeRepository;
        _eventPublisher = eventPublisher;
    }

    public Task<DeliveryDate?> GetDeliveryDateByIdAsync(int deliveryDateId)
    {
        return Task.FromResult(deliveryDateId == 0 ? null : _deliveryDateRepository.GetById(deliveryDateId));
    }

    public Task<IList<DeliveryDate>> GetAllDeliveryDatesAsync()
    {
        var query = from dd in _deliveryDateRepository.Table
                    orderby dd.DisplayOrder, dd.Id
                    select dd;
        return Task.FromResult<IList<DeliveryDate>>(query.ToList());
    }

    public async Task InsertDeliveryDateAsync(DeliveryDate deliveryDate)
    {
        ArgumentNullException.ThrowIfNull(deliveryDate);
        _deliveryDateRepository.Insert(deliveryDate);
        await _eventPublisher.EntityInsertedAsync(deliveryDate);
    }

    public async Task UpdateDeliveryDateAsync(DeliveryDate deliveryDate)
    {
        ArgumentNullException.ThrowIfNull(deliveryDate);
        _deliveryDateRepository.Update(deliveryDate);
        await _eventPublisher.EntityUpdatedAsync(deliveryDate);
    }

    public async Task DeleteDeliveryDateAsync(DeliveryDate deliveryDate)
    {
        ArgumentNullException.ThrowIfNull(deliveryDate);
        _deliveryDateRepository.Delete(deliveryDate);
        await _eventPublisher.EntityDeletedAsync(deliveryDate);
    }

    public Task<ProductAvailabilityRange?> GetProductAvailabilityRangeByIdAsync(int productAvailabilityRangeId)
    {
        return Task.FromResult(productAvailabilityRangeId == 0
            ? null
            : _productAvailabilityRangeRepository.GetById(productAvailabilityRangeId));
    }

    public Task<IList<ProductAvailabilityRange>> GetAllProductAvailabilityRangesAsync()
    {
        var query = from par in _productAvailabilityRangeRepository.Table
                    orderby par.DisplayOrder, par.Id
                    select par;
        return Task.FromResult<IList<ProductAvailabilityRange>>(query.ToList());
    }

    public async Task InsertProductAvailabilityRangeAsync(ProductAvailabilityRange productAvailabilityRange)
    {
        ArgumentNullException.ThrowIfNull(productAvailabilityRange);
        _productAvailabilityRangeRepository.Insert(productAvailabilityRange);
        await _eventPublisher.EntityInsertedAsync(productAvailabilityRange);
    }

    public async Task UpdateProductAvailabilityRangeAsync(ProductAvailabilityRange productAvailabilityRange)
    {
        ArgumentNullException.ThrowIfNull(productAvailabilityRange);
        _productAvailabilityRangeRepository.Update(productAvailabilityRange);
        await _eventPublisher.EntityUpdatedAsync(productAvailabilityRange);
    }

    public async Task DeleteProductAvailabilityRangeAsync(ProductAvailabilityRange productAvailabilityRange)
    {
        ArgumentNullException.ThrowIfNull(productAvailabilityRange);
        _productAvailabilityRangeRepository.Delete(productAvailabilityRange);
        await _eventPublisher.EntityDeletedAsync(productAvailabilityRange);
    }
}
