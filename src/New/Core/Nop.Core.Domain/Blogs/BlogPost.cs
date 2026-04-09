using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Stores;

namespace Nop.Core.Domain.Blogs
{

    public class BlogPost : BaseEntity, ISlugSupported, IStoreMappingSupported
    {
        public int LanguageId { get; set; }

        public string? Title { get; set; }

        public string? Body { get; set; }

        public string? BodyOverview { get; set; }

        public bool AllowComments { get; set; }

        public string? Tags { get; set; }

        public DateTime? StartDateUtc { get; set; }

        public DateTime? EndDateUtc { get; set; }

        public string? MetaKeywords { get; set; }

        public string? MetaDescription { get; set; }

        public string? MetaTitle { get; set; }

        public bool LimitedToStores { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}
