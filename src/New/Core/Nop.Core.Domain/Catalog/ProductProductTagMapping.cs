namespace Nop.Core.Domain.Catalog;

public class ProductProductTagMapping : BaseEntity
{
    public int ProductId { get; set; }
    public int ProductTagId { get; set; }
}
