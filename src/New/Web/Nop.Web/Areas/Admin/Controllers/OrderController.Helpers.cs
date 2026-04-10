using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Web.Areas.Admin.Models.Orders;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class OrderController
{
    private async Task<OrderModel> PrepareOrderModelAsync(Order order)
    {
        var customer = await customerService.GetCustomerByIdAsync(order.CustomerId);
        var store = await storeService.GetStoreByIdAsync(order.StoreId);

        var model = new OrderModel
        {
            Id = order.Id,
            OrderGuid = order.OrderGuid,
            CustomOrderNumber = order.CustomOrderNumber,
            StoreId = order.StoreId,
            StoreName = store?.Name,
            CustomerId = order.CustomerId,
            CustomerEmail = customer?.Email,
            CustomerIp = order.CustomerIp,
            VatNumber = order.VatNumber,
            AffiliateId = order.AffiliateId,
            OrderStatusName = order.OrderStatus.ToString(),
            OrderStatusId = order.OrderStatusId,
            PaymentStatusName = order.PaymentStatus.ToString(),
            PaymentStatusId = order.PaymentStatusId,
            ShippingStatusName = order.ShippingStatus.ToString(),
            ShippingStatusId = order.ShippingStatusId,
            PaymentMethod = order.PaymentMethodSystemName,
            OrderSubtotalInclTax = order.OrderSubtotalInclTax,
            OrderSubtotalExclTax = order.OrderSubtotalExclTax,
            OrderSubTotalDiscountInclTax = order.OrderSubTotalDiscountInclTax,
            OrderSubTotalDiscountExclTax = order.OrderSubTotalDiscountExclTax,
            OrderShippingInclTax = order.OrderShippingInclTax,
            OrderShippingExclTax = order.OrderShippingExclTax,
            PaymentMethodAdditionalFeeInclTax = order.PaymentMethodAdditionalFeeInclTax,
            PaymentMethodAdditionalFeeExclTax = order.PaymentMethodAdditionalFeeExclTax,
            OrderTax = order.OrderTax,
            OrderDiscount = order.OrderDiscount,
            OrderTotal = order.OrderTotal,
            RefundedAmount = order.RefundedAmount,
            TaxRates = order.TaxRates,
            ShippingMethod = order.ShippingMethod,
            IsShippable = order.ShippingStatus != ShippingStatus.ShippingNotRequired,
            CreatedOn = order.CreatedOnUtc,
            CanCancelOrder = orderProcessingService.CanCancelOrder(order),
            CanCapture = orderProcessingService.CanCapture(order),
            CanMarkOrderAsPaid = orderProcessingService.CanMarkOrderAsPaid(order),
            CanRefund = orderProcessingService.CanRefund(order),
            CanRefundOffline = orderProcessingService.CanRefundOffline(order),
            CanVoid = orderProcessingService.CanVoid(order),
            CanVoidOffline = orderProcessingService.CanVoidOffline(order)
        };

        // Billing address
        var billingAddress = await addressService.GetAddressByIdAsync(order.BillingAddressId);
        if (billingAddress is not null)
        {
            model.BillingFirstName = billingAddress.FirstName;
            model.BillingLastName = billingAddress.LastName;
            model.BillingEmail = billingAddress.Email;
            model.BillingPhone = billingAddress.PhoneNumber;
            model.BillingAddress1 = billingAddress.Address1;
            model.BillingCity = billingAddress.City;
            model.BillingZipPostalCode = billingAddress.ZipPostalCode;
            if (billingAddress.CountryId.HasValue)
            {
                var country = await countryService.GetCountryByIdAsync(billingAddress.CountryId.Value);
                model.BillingCountry = country?.Name;
            }
        }

        // Shipping address
        if (order.ShippingAddressId.HasValue)
        {
            var shippingAddress = await addressService.GetAddressByIdAsync(order.ShippingAddressId.Value);
            if (shippingAddress is not null)
            {
                model.ShippingFirstName = shippingAddress.FirstName;
                model.ShippingLastName = shippingAddress.LastName;
                model.ShippingPhone = shippingAddress.PhoneNumber;
                model.ShippingAddress1 = shippingAddress.Address1;
                model.ShippingCity = shippingAddress.City;
                model.ShippingZipPostalCode = shippingAddress.ZipPostalCode;
                if (shippingAddress.CountryId.HasValue)
                {
                    var country = await countryService.GetCountryByIdAsync(shippingAddress.CountryId.Value);
                    model.ShippingCountry = country?.Name;
                }
            }
        }

        // Order status dropdown
        foreach (var os in Enum.GetValues<OrderStatus>())
            model.AvailableOrderStatuses.Add(new SelectListItem
            {
                Text = os.ToString(),
                Value = ((int)os).ToString(),
                Selected = (int)os == order.OrderStatusId
            });

        // Order items
        var orderItems = await orderService.GetOrderItemsByOrderIdAsync(order.Id);
        foreach (var oi in orderItems)
        {
            var product = await productService.GetProductByIdAsync(oi.ProductId);
            model.Items.Add(new OrderItemModel
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = product?.Name,
                Sku = product?.Sku,
                UnitPriceInclTax = oi.UnitPriceInclTax,
                UnitPriceExclTax = oi.UnitPriceExclTax,
                Quantity = oi.Quantity,
                DiscountInclTax = oi.DiscountAmountInclTax,
                DiscountExclTax = oi.DiscountAmountExclTax,
                SubTotalInclTax = oi.PriceInclTax,
                SubTotalExclTax = oi.PriceExclTax
            });
        }

        return model;
    }

    private static List<int>? ParseStatusIds(string? statusIds)
    {
        if (string.IsNullOrWhiteSpace(statusIds))
            return null;

        var ids = statusIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        return ids.Count > 0 ? ids : null;
    }
}
