namespace Nop.Core.Domain.Customers
{

    public enum CustomerLoginResults
    {

        Successful = 1,

        CustomerNotExist = 2,

        WrongPassword = 3,

        NotActive = 4,

        Deleted = 5,

        NotRegistered = 6,

        LockedOut = 7,
    }
}
