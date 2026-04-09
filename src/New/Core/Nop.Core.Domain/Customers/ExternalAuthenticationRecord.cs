namespace Nop.Core.Domain.Customers
{

    public class ExternalAuthenticationRecord : BaseEntity
    {

        public int CustomerId { get; set; }

        public string? Email { get; set; }

        public string? ExternalIdentifier { get; set; }

        public string? ExternalDisplayIdentifier { get; set; }

        public string? OAuthToken { get; set; }

        public string? OAuthAccessToken { get; set; }

        public string? ProviderSystemName { get; set; }
    }

}
