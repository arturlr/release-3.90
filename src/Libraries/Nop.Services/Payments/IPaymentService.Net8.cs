using System.Threading.Tasks;

namespace Nop.Services.Payments
{
    public interface IPaymentService
    {
        Task<bool> ProcessPaymentAsync(int orderId);
        Task<bool> RefundAsync(int orderId);
        Task<bool> VoidAsync(int orderId);
    }
}
