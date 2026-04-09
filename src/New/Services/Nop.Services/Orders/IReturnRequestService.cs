using Nop.Core;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public interface IReturnRequestService
{
    Task DeleteReturnRequestAsync(ReturnRequest returnRequest);
    Task<ReturnRequest?> GetReturnRequestByIdAsync(int returnRequestId);
    Task<IPagedList<ReturnRequest>> SearchReturnRequestsAsync(int storeId = 0, int customerId = 0,
        int orderItemId = 0, string? customNumber = null, ReturnRequestStatus? rs = null,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertReturnRequestAsync(ReturnRequest returnRequest);
    Task UpdateReturnRequestAsync(ReturnRequest returnRequest);

    Task DeleteReturnRequestActionAsync(ReturnRequestAction returnRequestAction);
    Task<IList<ReturnRequestAction>> GetAllReturnRequestActionsAsync();
    Task<ReturnRequestAction?> GetReturnRequestActionByIdAsync(int returnRequestActionId);
    Task InsertReturnRequestActionAsync(ReturnRequestAction returnRequestAction);
    Task UpdateReturnRequestActionAsync(ReturnRequestAction returnRequestAction);

    Task DeleteReturnRequestReasonAsync(ReturnRequestReason returnRequestReason);
    Task<IList<ReturnRequestReason>> GetAllReturnRequestReasonsAsync();
    Task<ReturnRequestReason?> GetReturnRequestReasonByIdAsync(int returnRequestReasonId);
    Task InsertReturnRequestReasonAsync(ReturnRequestReason returnRequestReason);
    Task UpdateReturnRequestReasonAsync(ReturnRequestReason returnRequestReason);
}
