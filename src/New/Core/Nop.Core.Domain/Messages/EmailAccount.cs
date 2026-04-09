namespace Nop.Core.Domain.Messages
{

    public class EmailAccount : BaseEntity
    {

        public string? Email { get; set; }

        public string? DisplayName { get; set; }

        public string? Host { get; set; }

        public int Port { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public bool EnableSsl { get; set; }

        public bool UseDefaultCredentials { get; set; }

        public string? FriendlyName
        {
            get
            {
                if (!String.IsNullOrWhiteSpace(DisplayName))
                    return Email + " (" + DisplayName + ")";
                return Email;
            }
        }
    }
}
