using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Stores;

namespace Nop.Core.Domain.Topics
{

    public class Topic : BaseEntity, ILocalizedEntity, ISlugSupported, IStoreMappingSupported, IAclSupported
    {

        public string? SystemName { get; set; }

        public bool IncludeInSitemap { get; set; }

        public bool IncludeInTopMenu { get; set; }

        public bool IncludeInFooterColumn1 { get; set; }

        public bool IncludeInFooterColumn2 { get; set; }

        public bool IncludeInFooterColumn3 { get; set; }

        public int DisplayOrder { get; set; }

        public bool AccessibleWhenStoreClosed { get; set; }

        public bool IsPasswordProtected { get; set; }

        public string? Password { get; set; }

        public string? Title { get; set; }

        public string? Body { get; set; }

        public bool Published { get; set; }

        public int TopicTemplateId { get; set; }

        public string? MetaKeywords { get; set; }

        public string? MetaDescription { get; set; }

        public string? MetaTitle { get; set; }

        public bool SubjectToAcl { get; set; }

        public bool LimitedToStores { get; set; }
    }
}
