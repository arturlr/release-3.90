namespace Nop.Core.Domain.Tax
{

    public class TaxCategory : BaseEntity
    {

        public string? Name { get; set; }

        public int DisplayOrder { get; set; }
    }

}
