using Nop.Core.Domain.Tax;

namespace Nop.Services.Tax;

public interface ITaxCategoryService
{
    Task DeleteTaxCategoryAsync(TaxCategory taxCategory);
    Task<IList<TaxCategory>> GetAllTaxCategoriesAsync();
    Task<TaxCategory?> GetTaxCategoryByIdAsync(int taxCategoryId);
    Task InsertTaxCategoryAsync(TaxCategory taxCategory);
    Task UpdateTaxCategoryAsync(TaxCategory taxCategory);
}
