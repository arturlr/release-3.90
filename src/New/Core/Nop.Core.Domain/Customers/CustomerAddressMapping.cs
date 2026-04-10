namespace Nop.Core.Domain.Customers;

/// <summary>
/// Represents a customer-address mapping (many-to-many join entity).
/// Replaces legacy Customer.Addresses nav property.
/// </summary>
public class CustomerAddressMapping : BaseEntity
{
    public int CustomerId { get; set; }
    public int AddressId { get; set; }
}
