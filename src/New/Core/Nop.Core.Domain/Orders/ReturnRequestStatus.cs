namespace Nop.Core.Domain.Orders
{

    public enum ReturnRequestStatus
    {

        Pending = 0,

        Received = 10,

        ReturnAuthorized = 20,

        ItemsRepaired = 30,

        ItemsRefunded = 40,

        RequestRejected = 50,

        Cancelled = 60,
    }
}
