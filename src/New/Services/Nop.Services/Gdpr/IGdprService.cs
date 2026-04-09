using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Gdpr;

namespace Nop.Services.Gdpr;

public interface IGdprService
{
    Task<GdprLog?> GetLogByIdAsync(int logId);

    Task<IPagedList<GdprLog>> GetAllLogAsync(
        int customerId = 0, int requestTypeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue);

    Task InsertLogAsync(Customer customer, GdprRequestType requestType, string? requestDetails);

    Task DeleteLogAsync(GdprLog gdprLog);

    Task PermanentDeleteCustomerAsync(Customer customer);
}
