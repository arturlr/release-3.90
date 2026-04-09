using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Seo;

namespace Nop.Core.Domain.Vendors
{

    public class Vendor : BaseEntity, ILocalizedEntity, ISlugSupported
    {
        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Description { get; set; }

        public int PictureId { get; set; }

        public int AddressId { get; set; }

        public string? AdminComment { get; set; }

        public bool Active { get; set; }

        public bool Deleted { get; set; }

        public int DisplayOrder { get; set; }

        public string? MetaKeywords { get; set; }

        public string? MetaDescription { get; set; }

        public string? MetaTitle { get; set; }

        public int PageSize { get; set; }

        public bool AllowCustomersToSelectPageSize { get; set; }

        public string? PageSizeOptions { get; set; }
    }
}
