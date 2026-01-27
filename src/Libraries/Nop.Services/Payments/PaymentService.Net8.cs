using System.Threading.Tasks;

namespace Nop.Services.Payments
{
    public class PaymentService : IPaymentService
    {
        public virtual async Task<bool> ProcessPaymentAsync(int orderId)
        {
            // TODO: Implement payment processing
            return await Task.FromResult(true);
        }

        public virtual async Task<bool> RefundAsync(int orderId)
        {
            // TODO: Implement refund
            return await Task.FromResult(true);
        }

        public virtual async Task<bool> VoidAsync(int orderId)
        {
            // TODO: Implement void
            return await Task.FromResult(true);
        }
    }
}
