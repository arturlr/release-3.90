using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public class CustomNumberFormatter(OrderSettings orderSettings) : ICustomNumberFormatter
{
    public string GenerateOrderCustomNumber(Order order)
    {
        if (string.IsNullOrEmpty(orderSettings.CustomOrderNumberMask))
            return order.Id.ToString();

        return ReplaceMaskTokens(orderSettings.CustomOrderNumberMask, order.Id, order.CreatedOnUtc);
    }

    public string GenerateReturnRequestCustomNumber(ReturnRequest returnRequest)
    {
        if (string.IsNullOrEmpty(orderSettings.ReturnRequestNumberMask))
            return returnRequest.Id.ToString();

        return ReplaceMaskTokens(orderSettings.ReturnRequestNumberMask, returnRequest.Id, returnRequest.CreatedOnUtc);
    }

    private static string ReplaceMaskTokens(string mask, int id, DateTime dateUtc) =>
        mask.Replace("{ID}", id.ToString())
            .Replace("{YYYY}", dateUtc.ToString("yyyy"))
            .Replace("{YY}", dateUtc.ToString("yy"))
            .Replace("{MM}", dateUtc.ToString("MM"))
            .Replace("{DD}", dateUtc.ToString("dd"))
            .Trim();
}
