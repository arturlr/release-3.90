using Nop.Core.Configuration;

namespace Nop.Core.Domain.Orders;

public class OrderSettings : ISettings
{
    public bool IsReOrderAllowed { get; set; }
    public decimal MinOrderSubtotalAmount { get; set; }
    public bool MinOrderSubtotalAmountIncludingTax { get; set; }
    public decimal MinOrderTotalAmount { get; set; }
    public bool AutoUpdateOrderTotalsOnEditingOrder { get; set; }
    public bool AnonymousCheckoutAllowed { get; set; }
    public bool TermsOfServiceOnShoppingCartPage { get; set; }
    public bool TermsOfServiceOnOrderConfirmPage { get; set; }
    public bool OnePageCheckoutEnabled { get; set; }
    public bool OnePageCheckoutDisplayOrderTotalsOnPaymentInfoTab { get; set; }
    public bool DisableBillingAddressCheckoutStep { get; set; }
    public bool DisableOrderCompletedPage { get; set; }
    public bool AttachPdfInvoiceToOrderPlacedEmail { get; set; }
    public bool AttachPdfInvoiceToOrderPaidEmail { get; set; }
    public bool AttachPdfInvoiceToOrderCompletedEmail { get; set; }
    public bool GeneratePdfInvoiceInCustomerLanguage { get; set; }
    public bool ReturnRequestsEnabled { get; set; }
    public bool ReturnRequestsAllowFiles { get; set; }
    public int ReturnRequestsFileMaximumSize { get; set; }
    public string? ReturnRequestNumberMask { get; set; }
    public int NumberOfDaysReturnRequestAvailable { get; set; }
    public bool ActivateGiftCardsAfterCompletingOrder { get; set; }
    public bool DeactivateGiftCardsAfterCancellingOrder { get; set; }
    public bool DeactivateGiftCardsAfterDeletingOrder { get; set; }
    public int MinimumOrderPlacementInterval { get; set; }
    public bool CompleteOrderWhenDelivered { get; set; }
    public string? CustomOrderNumberMask { get; set; }
    public bool ExportWithProducts { get; set; }
}
