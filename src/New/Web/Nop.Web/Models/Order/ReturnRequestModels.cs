using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Order;

// --- Customer Return Requests List (My Account / Return Requests) ---

public class CustomerReturnRequestsModel : BaseNopModel
{
    public IList<ReturnRequestBriefModel> Items { get; set; } = [];

    public class ReturnRequestBriefModel : BaseNopEntityModel
    {
        public string? CustomNumber { get; set; }
        public string? ReturnRequestStatus { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSeName { get; set; }
        public int Quantity { get; set; }
        public string? ReturnReason { get; set; }
        public string? ReturnAction { get; set; }
        public string? Comments { get; set; }
        public Guid UploadedFileGuid { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}

// --- Submit Return Request Form ---

public class SubmitReturnRequestModel : BaseNopModel
{
    public int OrderId { get; set; }
    public string? CustomOrderNumber { get; set; }

    public IList<OrderItemModel> Items { get; set; } = [];
    public IList<ReturnRequestReasonModel> AvailableReturnReasons { get; set; } = [];
    public IList<ReturnRequestActionModel> AvailableReturnActions { get; set; } = [];

    public int ReturnRequestReasonId { get; set; }
    public int ReturnRequestActionId { get; set; }
    public string? Comments { get; set; }

    public bool AllowFiles { get; set; }
    public Guid UploadedFileGuid { get; set; }

    public string? Result { get; set; }

    public class OrderItemModel : BaseNopEntityModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSeName { get; set; }
        public string? AttributeInfo { get; set; }
        public string? UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class ReturnRequestReasonModel : BaseNopEntityModel
    {
        public string? Name { get; set; }
    }

    public class ReturnRequestActionModel : BaseNopEntityModel
    {
        public string? Name { get; set; }
    }
}
