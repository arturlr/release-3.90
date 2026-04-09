using Nop.Core.Domain.Stores;

namespace Nop.Core.Domain.Localization
{

    public class Language : BaseEntity, IStoreMappingSupported
    {
        public string? Name { get; set; }

        public string? LanguageCulture { get; set; }

        public string? UniqueSeoCode { get; set; }

        public string? FlagImageFileName { get; set; }

        public bool Rtl { get; set; }

        public bool LimitedToStores { get; set; }

        public int DefaultCurrencyId { get; set; }

        public bool Published { get; set; }

        public int DisplayOrder { get; set; }
    }
}
