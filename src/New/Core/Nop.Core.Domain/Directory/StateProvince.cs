using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Directory
{

    public class StateProvince : BaseEntity, ILocalizedEntity
    {

        public int CountryId { get; set; }

        public string? Name { get; set; }

        public string? Abbreviation { get; set; }

        public bool Published { get; set; }

        public int DisplayOrder { get; set; }
    }

}
