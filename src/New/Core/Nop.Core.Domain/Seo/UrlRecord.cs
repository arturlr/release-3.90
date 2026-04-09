namespace Nop.Core.Domain.Seo
{

    public class UrlRecord : BaseEntity
    {

        public int EntityId { get; set; }

        public string? EntityName { get; set; }

        public string? Slug { get; set; }

        public bool IsActive { get; set; }

        public int LanguageId { get; set; }
    }
}
