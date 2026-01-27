using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Shipping;
using Nop.Data;

namespace Nop.Services.Shipping
{
    public class ShippingService : IShippingService
    {
        private readonly IRepository<ShippingMethod> _shippingMethodRepository;

        public ShippingService(IRepository<ShippingMethod> shippingMethodRepository)
        {
            _shippingMethodRepository = shippingMethodRepository;
        }

        public virtual async Task<IList<ShippingMethod>> GetAllShippingMethodsAsync()
        {
            var query = _shippingMethodRepository.Table.OrderBy(sm => sm.DisplayOrder);
            return await query.ToListAsync();
        }

        public virtual async Task<ShippingMethod> GetShippingMethodByIdAsync(int shippingMethodId)
        {
            if (shippingMethodId == 0)
                return null;

            return await _shippingMethodRepository.GetByIdAsync(shippingMethodId);
        }

        public virtual async Task InsertShippingMethodAsync(ShippingMethod shippingMethod)
        {
            if (shippingMethod == null)
                throw new ArgumentNullException(nameof(shippingMethod));

            await _shippingMethodRepository.InsertAsync(shippingMethod);
        }

        public virtual async Task UpdateShippingMethodAsync(ShippingMethod shippingMethod)
        {
            if (shippingMethod == null)
                throw new ArgumentNullException(nameof(shippingMethod));

            await _shippingMethodRepository.UpdateAsync(shippingMethod);
        }

        public virtual async Task DeleteShippingMethodAsync(ShippingMethod shippingMethod)
        {
            if (shippingMethod == null)
                throw new ArgumentNullException(nameof(shippingMethod));

            await _shippingMethodRepository.DeleteAsync(shippingMethod);
        }
    }
}
