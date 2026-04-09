using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Stores;

namespace Nop.Core.Domain.Catalog
{

    public class Manufacturer : BaseEntity, ILocalizedEntity, ISlugSupported, IAclSupported, IStoreMappingSupported
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public int ManufacturerTemplateId { get; set; }

        public string? MetaKeywords { get; set; }

        public string? MetaDescription { get; set; }

        public string? MetaTitle { get; set; }

        public int PictureId { get; set; }

        public int PageSize { get; set; }

        public bool AllowCustomersToSelectPageSize { get; set; }

        public string? PageSizeOptions { get; set; }

        public string? PriceRanges { get; set; }

        public bool SubjectToAcl { get; set; }

        public bool LimitedToStores { get; set; }

        public bool Published { get; set; }

        public bool Deleted { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime UpdatedOnUtc { get; set; }
    }
}
