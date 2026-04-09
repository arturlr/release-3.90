namespace Nop.Core.Domain.Catalog
{

    public class ProductTemplate : BaseEntity
    {

        public string? Name { get; set; }

        public string? ViewPath { get; set; }

        public int DisplayOrder { get; set; }

        public string? IgnoredProductTypes { get; set; }
    }
}
