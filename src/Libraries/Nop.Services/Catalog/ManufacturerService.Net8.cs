using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Catalog;
using Nop.Data;

namespace Nop.Services.Catalog
{
    public class ManufacturerService : IManufacturerService
    {
        private readonly IRepository<Manufacturer> _manufacturerRepository;

        public ManufacturerService(IRepository<Manufacturer> manufacturerRepository)
        {
            _manufacturerRepository = manufacturerRepository;
        }

        public virtual async Task<Manufacturer> GetManufacturerByIdAsync(int manufacturerId)
        {
            if (manufacturerId == 0)
                return null;

            return await _manufacturerRepository.GetByIdAsync(manufacturerId);
        }

        public virtual async Task<IList<Manufacturer>> GetAllManufacturersAsync()
        {
            var query = _manufacturerRepository.Table
                .Where(m => !m.Deleted && m.Published)
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name);

            return await query.ToListAsync();
        }

        public virtual async Task InsertManufacturerAsync(Manufacturer manufacturer)
        {
            if (manufacturer == null)
                throw new ArgumentNullException(nameof(manufacturer));

            await _manufacturerRepository.InsertAsync(manufacturer);
        }

        public virtual async Task UpdateManufacturerAsync(Manufacturer manufacturer)
        {
            if (manufacturer == null)
                throw new ArgumentNullException(nameof(manufacturer));

            await _manufacturerRepository.UpdateAsync(manufacturer);
        }

        public virtual async Task DeleteManufacturerAsync(Manufacturer manufacturer)
        {
            if (manufacturer == null)
                throw new ArgumentNullException(nameof(manufacturer));

            manufacturer.Deleted = true;
            await _manufacturerRepository.UpdateAsync(manufacturer);
        }
    }
}
