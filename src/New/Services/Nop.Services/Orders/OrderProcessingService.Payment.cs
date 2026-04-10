using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Payments;

namespace Nop.Services.Orders;

public partial class OrderProcessingService
{
    #region Authorize

    public virtual bool CanMarkOrderAsAuthorized(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order.OrderStatus != OrderStatus.Cancelled && order.PaymentStatus == PaymentStatus.Pending;
    }

    public virtual async Task MarkAsAuthorizedAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        order.PaymentStatusId = (int)PaymentStatus.Authorized;
        await orderService.UpdateOrderAsync(order);
        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = "Order has been marked as authorized",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });
        await CheckOrderStatusAsync(order);
    }

    #endregion

    #region Capture

    public virtual bool CanCapture(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.OrderStatus is OrderStatus.Cancelled or OrderStatus.Pending) return false;
        return order.PaymentStatus == PaymentStatus.Authorized &&
               paymentService.SupportCaptureAsync(order.PaymentMethodSystemName!).GetAwaiter().GetResult();
    }

    public virtual async Task<IList<string>> CaptureAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!CanCapture(order))
            throw new NopException("Cannot do capture for order.");

        CapturePaymentResult? result = null;
        try
        {
            result = await paymentService.CaptureAsync(new CapturePaymentRequest { Order = order });
            if (result.Success)
            {
                if (result.NewPaymentStatus == PaymentStatus.Paid)
                    order.PaidDateUtc = DateTime.UtcNow;
                order.CaptureTransactionId = result.CaptureTransactionId;
                order.CaptureTransactionResult = result.CaptureTransactionResult;
                order.PaymentStatus = result.NewPaymentStatus;
                await orderService.UpdateOrderAsync(order);
                await orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "Order has been captured",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                await CheckOrderStatusAsync(order);
                if (order.PaymentStatus == PaymentStatus.Paid)
                    await ProcessOrderPaidAsync(order);
            }
        }
        catch (Exception exc)
        {
            result ??= new CapturePaymentResult();
            result.AddError($"Error: {exc.Message}");
        }

        if (result.Errors.Count > 0)
        {
            var error = string.Join(". ", result.Errors.Select((e, i) => $"Error {i}: {e}"));
            await orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = $"Unable to capture order. {error}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            logger.Error($"Error capturing order #{order.Id}. Error: {error}");
        }
        return result.Errors;
    }

    #endregion

    #region Mark as Paid

    public virtual bool CanMarkOrderAsPaid(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.OrderStatus == OrderStatus.Cancelled) return false;
        return order.PaymentStatus is not (PaymentStatus.Paid or PaymentStatus.Refunded or PaymentStatus.Voided);
    }

    public virtual async Task MarkOrderAsPaidAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!CanMarkOrderAsPaid(order))
            throw new NopException("You can't mark this order as paid");

        order.PaymentStatusId = (int)PaymentStatus.Paid;
        order.PaidDateUtc = DateTime.UtcNow;
        await orderService.UpdateOrderAsync(order);
        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = "Order has been marked as paid",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });
        await CheckOrderStatusAsync(order);
        if (order.PaymentStatus == PaymentStatus.Paid)
            await ProcessOrderPaidAsync(order);
    }

    #endregion

    #region Refund

    public virtual bool CanRefund(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.OrderTotal == decimal.Zero) return false;
        if (order.RefundedAmount > decimal.Zero) return false;
        return order.PaymentStatus == PaymentStatus.Paid &&
               paymentService.SupportRefundAsync(order.PaymentMethodSystemName!).GetAwaiter().GetResult();
    }

    public virtual async Task<IList<string>> RefundAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!CanRefund(order))
            throw new NopException("Cannot do refund for order.");

        var request = new RefundPaymentRequest { Order = order, AmountToRefund = order.OrderTotal, IsPartialRefund = false };
        RefundPaymentResult? result = null;
        try
        {
            result = await paymentService.RefundAsync(request);
            if (result.Success)
            {
                order.RefundedAmount = order.RefundedAmount + request.AmountToRefund;
                order.PaymentStatus = result.NewPaymentStatus;
                await orderService.UpdateOrderAsync(order);
                await orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = $"Order has been refunded. Amount = {request.AmountToRefund}",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                await CheckOrderStatusAsync(order);
                await workflowMessageService.SendOrderRefundedStoreOwnerNotificationAsync(order, request.AmountToRefund, localizationSettings.DefaultAdminLanguageId);
                await workflowMessageService.SendOrderRefundedCustomerNotificationAsync(order, request.AmountToRefund, order.CustomerLanguageId);
                await eventPublisher.PublishAsync(new OrderRefundedEvent(order, request.AmountToRefund));
            }
        }
        catch (Exception exc)
        {
            result ??= new RefundPaymentResult();
            result.AddError($"Error: {exc.Message}");
        }

        if (result.Errors.Count > 0)
        {
            var error = string.Join(". ", result.Errors.Select((e, i) => $"Error {i}: {e}"));
            await orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = $"Unable to refund order. {error}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            logger.Error($"Error refunding order #{order.Id}. Error: {error}");
        }
        return result.Errors;
    }

    public virtual bool CanRefundOffline(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.OrderTotal == decimal.Zero) return false;
        if (order.RefundedAmount > decimal.Zero) return false;
        return order.PaymentStatus == PaymentStatus.Paid;
    }

    public virtual async Task RefundOfflineAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!CanRefundOffline(order))
            throw new NopException("You can't refund this order");

        var amountToRefund = order.OrderTotal;
        order.RefundedAmount = order.RefundedAmount + amountToRefund;
        order.PaymentStatus = PaymentStatus.Refunded;
        await orderService.UpdateOrderAsync(order);
        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = $"Order has been marked as refunded. Amount = {amountToRefund}",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });
        await CheckOrderStatusAsync(order);
        await workflowMessageService.SendOrderRefundedStoreOwnerNotificationAsync(order, amountToRefund, localizationSettings.DefaultAdminLanguageId);
        await workflowMessageService.SendOrderRefundedCustomerNotificationAsync(order, amountToRefund, order.CustomerLanguageId);
        await eventPublisher.PublishAsync(new OrderRefundedEvent(order, amountToRefund));
    }

    #endregion

    #region Partial Refund

    public virtual bool CanPartiallyRefund(Order order, decimal amountToRefund)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.OrderTotal == decimal.Zero) return false;
        var canBeRefunded = order.OrderTotal - order.RefundedAmount;
        if (canBeRefunded <= decimal.Zero || amountToRefund > canBeRefunded) return false;
        return (order.PaymentStatus is PaymentStatus.Paid or PaymentStatus.PartiallyRefunded) &&
               paymentService.SupportPartiallyRefundAsync(order.PaymentMethodSystemName!).GetAwaiter().GetResult();
    }

    public virtual async Task<IList<string>> PartiallyRefundAsync(Order order, decimal amountToRefund)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!CanPartiallyRefund(order, amountToRefund))
            throw new NopException("Cannot do partial refund for order.");

        var request = new RefundPaymentRequest { Order = order, AmountToRefund = amountToRefund, IsPartialRefund = true };
        RefundPaymentResult? result = null;
        try
        {
            result = await paymentService.RefundAsync(request);
            if (result.Success)
            {
                var totalRefunded = order.RefundedAmount + amountToRefund;
                order.RefundedAmount = totalRefunded;
                order.PaymentStatus = order.OrderTotal == totalRefunded && result.NewPaymentStatus == PaymentStatus.PartiallyRefunded
                    ? PaymentStatus.Refunded : result.NewPaymentStatus;
                await orderService.UpdateOrderAsync(order);
                await orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = $"Order has been partially refunded. Amount = {amountToRefund}",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                await CheckOrderStatusAsync(order);
                await workflowMessageService.SendOrderRefundedStoreOwnerNotificationAsync(order, amountToRefund, localizationSettings.DefaultAdminLanguageId);
                await workflowMessageService.SendOrderRefundedCustomerNotificationAsync(order, amountToRefund, order.CustomerLanguageId);
                await eventPublisher.PublishAsync(new OrderRefundedEvent(order, amountToRefund));
            }
        }
        catch (Exception exc)
        {
            result ??= new RefundPaymentResult();
            result.AddError($"Error: {exc.Message}");
        }

        if (result.Errors.Count > 0)
        {
            var error = string.Join(". ", result.Errors.Select((e, i) => $"Error {i}: {e}"));
            await orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = $"Unable to partially refund order. {error}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            logger.Error($"Error refunding order #{order.Id}. Error: {error}");
        }
        return result.Errors;
    }

    public virtual bool CanPartiallyRefundOffline(Order order, decimal amountToRefund)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.OrderTotal == decimal.Zero) return false;
        var canBeRefunded = order.OrderTotal - order.RefundedAmount;
        if (canBeRefunded <= decimal.Zero || amountToRefund > canBeRefunded) return false;
        return order.PaymentStatus is PaymentStatus.Paid or PaymentStatus.PartiallyRefunded;
    }

    public virtual async Task PartiallyRefundOfflineAsync(Order order, decimal amountToRefund)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!CanPartiallyRefundOffline(order, amountToRefund))
            throw new NopException("You can't partially refund (offline) this order");

        var totalRefunded = order.RefundedAmount + amountToRefund;
        order.RefundedAmount = totalRefunded;
        order.PaymentStatus = order.OrderTotal == totalRefunded ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        await orderService.UpdateOrderAsync(order);
        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = $"Order has been marked as partially refunded. Amount = {amountToRefund}",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });
        await CheckOrderStatusAsync(order);
        await workflowMessageService.SendOrderRefundedStoreOwnerNotificationAsync(order, amountToRefund, localizationSettings.DefaultAdminLanguageId);
        await workflowMessageService.SendOrderRefundedCustomerNotificationAsync(order, amountToRefund, order.CustomerLanguageId);
        await eventPublisher.PublishAsync(new OrderRefundedEvent(order, amountToRefund));
    }

    #endregion

    #region Void

    public virtual bool CanVoid(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.OrderTotal == decimal.Zero) return false;
        return order.PaymentStatus == PaymentStatus.Authorized &&
               paymentService.SupportVoidAsync(order.PaymentMethodSystemName!).GetAwaiter().GetResult();
    }

    public virtual async Task<IList<string>> VoidAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!CanVoid(order))
            throw new NopException("Cannot do void for order.");

        VoidPaymentResult? result = null;
        try
        {
            result = await paymentService.VoidAsync(new VoidPaymentRequest { Order = order });
            if (result.Success)
            {
                order.PaymentStatus = result.NewPaymentStatus;
                await orderService.UpdateOrderAsync(order);
                await orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "Order has been voided",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });
                await CheckOrderStatusAsync(order);
            }
        }
        catch (Exception exc)
        {
            result ??= new VoidPaymentResult();
            result.AddError($"Error: {exc.Message}");
        }

        if (result.Errors.Count > 0)
        {
            var error = string.Join(". ", result.Errors.Select((e, i) => $"Error {i}: {e}"));
            await orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = $"Unable to void order. {error}",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            logger.Error($"Error voiding order #{order.Id}. Error: {error}");
        }
        return result.Errors;
    }

    public virtual bool CanVoidOffline(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.OrderTotal == decimal.Zero) return false;
        return order.PaymentStatus == PaymentStatus.Authorized;
    }

    public virtual async Task VoidOfflineAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!CanVoidOffline(order))
            throw new NopException("You can't void this order");

        order.PaymentStatusId = (int)PaymentStatus.Voided;
        await orderService.UpdateOrderAsync(order);
        await orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = "Order has been marked as voided",
            DisplayToCustomer = false,
            CreatedOnUtc = DateTime.UtcNow
        });
        await CheckOrderStatusAsync(order);
    }

    #endregion
}
