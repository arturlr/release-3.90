namespace Nop.Core.Domain.Localization
{

    public class LocaleStringResource : BaseEntity
    {

        public int LanguageId { get; set; }

        public string? ResourceName { get; set; }

        public string? ResourceValue { get; set; }
    }

}
