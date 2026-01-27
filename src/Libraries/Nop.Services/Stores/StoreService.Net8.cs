using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Stores;
using Nop.Data;

namespace Nop.Services.Stores
{
    public class StoreService : IStoreService
    {
        private readonly IRepository<Store> _storeRepository;

        public StoreService(IRepository<Store> storeRepository)
        {
            _storeRepository = storeRepository;
        }

        public virtual async Task<Store> GetStoreByIdAsync(int storeId)
        {
            if (storeId == 0)
                return null;

            return await _storeRepository.GetByIdAsync(storeId);
        }

        public virtual async Task<IList<Store>> GetAllStoresAsync()
        {
            var query = _storeRepository.Table.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name);
            return await query.ToListAsync();
        }

        public virtual async Task InsertStoreAsync(Store store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            await _storeRepository.InsertAsync(store);
        }

        public virtual async Task UpdateStoreAsync(Store store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            await _storeRepository.UpdateAsync(store);
        }

        public virtual async Task DeleteStoreAsync(Store store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            await _storeRepository.DeleteAsync(store);
        }
    }
}
