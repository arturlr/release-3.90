using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Stores;

namespace Nop.Core.Domain.News
{

    public class NewsItem : BaseEntity, ISlugSupported, IStoreMappingSupported
    {
        public int LanguageId { get; set; }

        public string? Title { get; set; }

        public string? Short { get; set; }

        public string? Full { get; set; }

        public bool Published { get; set; }

        public DateTime? StartDateUtc { get; set; }

        public DateTime? EndDateUtc { get; set; }

        public bool AllowComments { get; set; }

        public bool LimitedToStores { get; set; }

        public string? MetaKeywords { get; set; }

        public string? MetaDescription { get; set; }

        public string? MetaTitle { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}
