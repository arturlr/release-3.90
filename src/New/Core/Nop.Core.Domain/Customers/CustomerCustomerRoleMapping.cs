namespace Nop.Core.Domain.Customers;

/// <summary>
/// Join entity for Customer ↔ CustomerRole many-to-many (table: Customer_CustomerRole_Mapping).
/// </summary>
public class CustomerCustomerRoleMapping : BaseEntity
{
    public int CustomerId { get; set; }
    public int CustomerRoleId { get; set; }
}
