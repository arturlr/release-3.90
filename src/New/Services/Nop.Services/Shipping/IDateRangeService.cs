using Nop.Core.Domain.Shipping;

namespace Nop.Services.Shipping;

public interface IDateRangeService
{
    Task<DeliveryDate?> GetDeliveryDateByIdAsync(int deliveryDateId);
    Task<IList<DeliveryDate>> GetAllDeliveryDatesAsync();
    Task InsertDeliveryDateAsync(DeliveryDate deliveryDate);
    Task UpdateDeliveryDateAsync(DeliveryDate deliveryDate);
    Task DeleteDeliveryDateAsync(DeliveryDate deliveryDate);

    Task<ProductAvailabilityRange?> GetProductAvailabilityRangeByIdAsync(int productAvailabilityRangeId);
    Task<IList<ProductAvailabilityRange>> GetAllProductAvailabilityRangesAsync();
    Task InsertProductAvailabilityRangeAsync(ProductAvailabilityRange productAvailabilityRange);
    Task UpdateProductAvailabilityRangeAsync(ProductAvailabilityRange productAvailabilityRange);
    Task DeleteProductAvailabilityRangeAsync(ProductAvailabilityRange productAvailabilityRange);
}
