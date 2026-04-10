using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Orders;

public class ReturnRequestListModel : BaseNopModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? CustomNumber { get; set; }
    public int ReturnRequestStatusId { get; set; } = -1;
}

public class ReturnRequestModel : BaseNopEntityModel
{
    public string CustomNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string CustomOrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerInfo { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string AttributeInfo { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ReasonForReturn { get; set; }
    public string? RequestedAction { get; set; }
    public string? CustomerComments { get; set; }
    public Guid UploadedFileGuid { get; set; }
    public string? StaffNotes { get; set; }
    public int ReturnRequestStatusId { get; set; }
    public string ReturnRequestStatusStr { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}

public class ReturnRequestGridModel : BaseNopEntityModel
{
    public string CustomNumber { get; set; } = string.Empty;
    public string CustomOrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerInfo { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string ReturnRequestStatusStr { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}
