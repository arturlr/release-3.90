using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Tax;

public class CalculateTaxRequest
{
    public Customer? Customer { get; set; }
    public Product? Product { get; set; }
    public Address? Address { get; set; }
    public int TaxCategoryId { get; set; }
    public decimal Price { get; set; }
}
