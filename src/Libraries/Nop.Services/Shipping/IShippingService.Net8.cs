using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Shipping
{
    public interface IShippingService
    {
        Task<IList<ShippingMethod>> GetAllShippingMethodsAsync();
        Task<ShippingMethod> GetShippingMethodByIdAsync(int shippingMethodId);
        Task InsertShippingMethodAsync(ShippingMethod shippingMethod);
        Task UpdateShippingMethodAsync(ShippingMethod shippingMethod);
        Task DeleteShippingMethodAsync(ShippingMethod shippingMethod);
    }
}
