using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    public interface IManufacturerService
    {
        Task<Manufacturer> GetManufacturerByIdAsync(int manufacturerId);
        Task<IList<Manufacturer>> GetAllManufacturersAsync();
        Task InsertManufacturerAsync(Manufacturer manufacturer);
        Task UpdateManufacturerAsync(Manufacturer manufacturer);
        Task DeleteManufacturerAsync(Manufacturer manufacturer);
    }
}
