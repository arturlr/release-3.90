using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Common
{

    public class AddressAttributeValue : BaseEntity, ILocalizedEntity
    {

        public int AddressAttributeId { get; set; }

        public string? Name { get; set; }

        public bool IsPreSelected { get; set; }

        public int DisplayOrder { get; set; }
    }

}
