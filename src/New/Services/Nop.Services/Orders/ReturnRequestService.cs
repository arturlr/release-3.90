using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;

namespace Nop.Services.Orders;

public class ReturnRequestService(
    IRepository<ReturnRequest> returnRequestRepository,
    IRepository<ReturnRequestAction> returnRequestActionRepository,
    IRepository<ReturnRequestReason> returnRequestReasonRepository,
    IEventPublisher eventPublisher) : IReturnRequestService
{
    public Task DeleteReturnRequestAsync(ReturnRequest returnRequest)
    {
        ArgumentNullException.ThrowIfNull(returnRequest);
        returnRequestRepository.Delete(returnRequest);
        return eventPublisher.EntityDeletedAsync(returnRequest);
    }

    public Task<ReturnRequest?> GetReturnRequestByIdAsync(int returnRequestId) =>
        Task.FromResult(returnRequestId == 0 ? null : (ReturnRequest?)returnRequestRepository.GetById(returnRequestId));

    public Task<IPagedList<ReturnRequest>> SearchReturnRequestsAsync(int storeId = 0, int customerId = 0,
        int orderItemId = 0, string? customNumber = null, ReturnRequestStatus? rs = null,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = returnRequestRepository.Table;

        if (storeId > 0) query = query.Where(rr => rr.StoreId == storeId);
        if (customerId > 0) query = query.Where(rr => rr.CustomerId == customerId);
        if (orderItemId > 0) query = query.Where(rr => rr.OrderItemId == orderItemId);
        if (!string.IsNullOrEmpty(customNumber)) query = query.Where(rr => rr.CustomNumber == customNumber);
        if (rs.HasValue) query = query.Where(rr => rr.ReturnRequestStatusId == (int)rs.Value);
        if (createdFromUtc.HasValue) query = query.Where(rr => rr.CreatedOnUtc >= createdFromUtc.Value);
        if (createdToUtc.HasValue) query = query.Where(rr => rr.CreatedOnUtc <= createdToUtc.Value);

        query = query.OrderByDescending(rr => rr.CreatedOnUtc).ThenByDescending(rr => rr.Id);

        return Task.FromResult<IPagedList<ReturnRequest>>(new PagedList<ReturnRequest>(query, pageIndex, pageSize));
    }

    public Task InsertReturnRequestAsync(ReturnRequest returnRequest)
    {
        ArgumentNullException.ThrowIfNull(returnRequest);
        returnRequestRepository.Insert(returnRequest);
        return eventPublisher.EntityInsertedAsync(returnRequest);
    }

    public Task UpdateReturnRequestAsync(ReturnRequest returnRequest)
    {
        ArgumentNullException.ThrowIfNull(returnRequest);
        returnRequestRepository.Update(returnRequest);
        return eventPublisher.EntityUpdatedAsync(returnRequest);
    }

    // Return request actions
    public Task DeleteReturnRequestActionAsync(ReturnRequestAction returnRequestAction)
    {
        ArgumentNullException.ThrowIfNull(returnRequestAction);
        returnRequestActionRepository.Delete(returnRequestAction);
        return eventPublisher.EntityDeletedAsync(returnRequestAction);
    }

    public Task<IList<ReturnRequestAction>> GetAllReturnRequestActionsAsync() =>
        Task.FromResult<IList<ReturnRequestAction>>(returnRequestActionRepository.Table.OrderBy(a => a.DisplayOrder).ThenBy(a => a.Id).ToList());

    public Task<ReturnRequestAction?> GetReturnRequestActionByIdAsync(int returnRequestActionId) =>
        Task.FromResult(returnRequestActionId == 0 ? null : (ReturnRequestAction?)returnRequestActionRepository.GetById(returnRequestActionId));

    public Task InsertReturnRequestActionAsync(ReturnRequestAction returnRequestAction)
    {
        ArgumentNullException.ThrowIfNull(returnRequestAction);
        returnRequestActionRepository.Insert(returnRequestAction);
        return eventPublisher.EntityInsertedAsync(returnRequestAction);
    }

    public Task UpdateReturnRequestActionAsync(ReturnRequestAction returnRequestAction)
    {
        ArgumentNullException.ThrowIfNull(returnRequestAction);
        returnRequestActionRepository.Update(returnRequestAction);
        return eventPublisher.EntityUpdatedAsync(returnRequestAction);
    }

    // Return request reasons
    public Task DeleteReturnRequestReasonAsync(ReturnRequestReason returnRequestReason)
    {
        ArgumentNullException.ThrowIfNull(returnRequestReason);
        returnRequestReasonRepository.Delete(returnRequestReason);
        return eventPublisher.EntityDeletedAsync(returnRequestReason);
    }

    public Task<IList<ReturnRequestReason>> GetAllReturnRequestReasonsAsync() =>
        Task.FromResult<IList<ReturnRequestReason>>(returnRequestReasonRepository.Table.OrderBy(r => r.DisplayOrder).ThenBy(r => r.Id).ToList());

    public Task<ReturnRequestReason?> GetReturnRequestReasonByIdAsync(int returnRequestReasonId) =>
        Task.FromResult(returnRequestReasonId == 0 ? null : (ReturnRequestReason?)returnRequestReasonRepository.GetById(returnRequestReasonId));

    public Task InsertReturnRequestReasonAsync(ReturnRequestReason returnRequestReason)
    {
        ArgumentNullException.ThrowIfNull(returnRequestReason);
        returnRequestReasonRepository.Insert(returnRequestReason);
        return eventPublisher.EntityInsertedAsync(returnRequestReason);
    }

    public Task UpdateReturnRequestReasonAsync(ReturnRequestReason returnRequestReason)
    {
        ArgumentNullException.ThrowIfNull(returnRequestReason);
        returnRequestReasonRepository.Update(returnRequestReason);
        return eventPublisher.EntityUpdatedAsync(returnRequestReason);
    }
}
