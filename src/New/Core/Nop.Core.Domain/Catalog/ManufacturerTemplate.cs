namespace Nop.Core.Domain.Catalog
{

    public class ManufacturerTemplate : BaseEntity
    {

        public string? Name { get; set; }

        public string? ViewPath { get; set; }

        public int DisplayOrder { get; set; }
    }
}
