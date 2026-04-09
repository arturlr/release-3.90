using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Orders
{

    public class ReturnRequestAction : BaseEntity, ILocalizedEntity
    {

        public string? Name { get; set; }

        public int DisplayOrder { get; set; }
    }
}
