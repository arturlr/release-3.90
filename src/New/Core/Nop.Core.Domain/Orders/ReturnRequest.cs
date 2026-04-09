namespace Nop.Core.Domain.Orders
{

    public class ReturnRequest : BaseEntity
    {

        public string? CustomNumber { get; set; }

        public int StoreId { get; set; }

        public int OrderItemId { get; set; }

        public int CustomerId { get; set; }

        public int Quantity { get; set; }

        public string? ReasonForReturn { get; set; }

        public string? RequestedAction { get; set; }

        public string? CustomerComments { get; set; }

        public int UploadedFileId { get; set; }

        public string? StaffNotes { get; set; }

        public int ReturnRequestStatusId { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime UpdatedOnUtc { get; set; }

        public ReturnRequestStatus ReturnRequestStatus
        {
            get
            {
                return (ReturnRequestStatus)ReturnRequestStatusId;
            }
            set
            {
                ReturnRequestStatusId = (int)value;
            }
        }
    }
}
