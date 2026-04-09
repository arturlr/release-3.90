using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IManufacturerTemplateService
{
    Task<ManufacturerTemplate?> GetManufacturerTemplateByIdAsync(int manufacturerTemplateId);
    Task<IList<ManufacturerTemplate>> GetAllManufacturerTemplatesAsync();
    Task InsertManufacturerTemplateAsync(ManufacturerTemplate manufacturerTemplate);
    Task UpdateManufacturerTemplateAsync(ManufacturerTemplate manufacturerTemplate);
    Task DeleteManufacturerTemplateAsync(ManufacturerTemplate manufacturerTemplate);
}
