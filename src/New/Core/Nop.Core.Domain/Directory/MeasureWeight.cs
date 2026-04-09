namespace Nop.Core.Domain.Directory
{

    public class MeasureWeight : BaseEntity
    {

        public string? Name { get; set; }

        public string? SystemKeyword { get; set; }

        public decimal Ratio { get; set; }

        public int DisplayOrder { get; set; }
    }
}
