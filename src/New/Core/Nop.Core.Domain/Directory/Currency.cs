using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Stores;

namespace Nop.Core.Domain.Directory
{

    public class Currency : BaseEntity, ILocalizedEntity, IStoreMappingSupported
    {

        public string? Name { get; set; }

        public string? CurrencyCode { get; set; }

        public decimal Rate { get; set; }

        public string? DisplayLocale { get; set; }

        public string? CustomFormatting { get; set; }

        public bool LimitedToStores { get; set; }

        public bool Published { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime UpdatedOnUtc { get; set; }

        public int RoundingTypeId { get; set; }

        public RoundingType RoundingType
        {
            get
            {
                return (RoundingType)RoundingTypeId;
            }
            set
            {
                RoundingTypeId = (int)value;
            }
        }
    }

}
