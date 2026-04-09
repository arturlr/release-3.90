using Nop.Core.Domain.Localization;

namespace Nop.Core.Domain.Stores
{

    public class Store : BaseEntity, ILocalizedEntity
    {

        public string? Name { get; set; }

        public string? Url { get; set; }

        public bool SslEnabled { get; set; }

        public string? SecureUrl { get; set; }

        public string? Hosts { get; set; }

        public int DefaultLanguageId { get; set; }

        public int DisplayOrder { get; set; }

        public string? CompanyName { get; set; }

        public string? CompanyAddress { get; set; }

        public string? CompanyPhoneNumber { get; set; }

        public string? CompanyVat { get; set; }
    }
}
