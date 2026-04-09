namespace Nop.Core.Domain.Customers
{

    public class CustomerPassword : BaseEntity
    {
        public CustomerPassword()
        {
            PasswordFormat = PasswordFormat.Clear;
        }

        public int CustomerId { get; set; }

        public string? Password { get; set; }

        public int PasswordFormatId { get; set; }

        public string? PasswordSalt { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public PasswordFormat PasswordFormat
        {
            get { return (PasswordFormat)PasswordFormatId; }
            set { PasswordFormatId = (int)value; }
        }
    }
}
