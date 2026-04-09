using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IManufacturerService
{
    Task DeleteManufacturerAsync(Manufacturer manufacturer);
    Task<IPagedList<Manufacturer>> GetAllManufacturersAsync(string manufacturerName = "", int storeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    Task<Manufacturer?> GetManufacturerByIdAsync(int manufacturerId);
    Task InsertManufacturerAsync(Manufacturer manufacturer);
    Task UpdateManufacturerAsync(Manufacturer manufacturer);

    Task DeleteProductManufacturerAsync(ProductManufacturer productManufacturer);
    Task<IPagedList<ProductManufacturer>> GetProductManufacturersByManufacturerIdAsync(int manufacturerId,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    Task<IList<ProductManufacturer>> GetProductManufacturersByProductIdAsync(int productId, bool showHidden = false);
    Task<ProductManufacturer?> GetProductManufacturerByIdAsync(int productManufacturerId);
    Task InsertProductManufacturerAsync(ProductManufacturer productManufacturer);
    Task UpdateProductManufacturerAsync(ProductManufacturer productManufacturer);

    Task<IDictionary<int, int[]>> GetProductManufacturerIdsAsync(int[] productIds);
    Task<string[]> GetNotExistingManufacturersAsync(string[] manufacturerNames);
}
