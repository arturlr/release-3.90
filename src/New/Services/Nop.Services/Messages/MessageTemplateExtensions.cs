using Nop.Core.Domain.Messages;

namespace Nop.Services.Messages;

public static class MessageTemplateExtensions
{
    public static IEnumerable<string> GetTokenGroups(this MessageTemplate messageTemplate)
    {
        return messageTemplate.Name switch
        {
            MessageTemplateSystemNames.CustomerRegisteredNotification or
            MessageTemplateSystemNames.CustomerWelcomeMessage or
            MessageTemplateSystemNames.CustomerEmailValidationMessage or
            MessageTemplateSystemNames.CustomerEmailRevalidationMessage or
            MessageTemplateSystemNames.CustomerPasswordRecoveryMessage
                => [TokenGroupNames.StoreTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.OrderPlacedVendorNotification or
            MessageTemplateSystemNames.OrderPlacedStoreOwnerNotification or
            MessageTemplateSystemNames.OrderPaidStoreOwnerNotification or
            MessageTemplateSystemNames.OrderPaidCustomerNotification or
            MessageTemplateSystemNames.OrderPaidVendorNotification or
            MessageTemplateSystemNames.OrderPlacedCustomerNotification or
            MessageTemplateSystemNames.OrderCompletedCustomerNotification or
            MessageTemplateSystemNames.OrderCancelledCustomerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.OrderTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.ShipmentSentCustomerNotification or
            MessageTemplateSystemNames.ShipmentDeliveredCustomerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.ShipmentTokens, TokenGroupNames.OrderTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.OrderRefundedStoreOwnerNotification or
            MessageTemplateSystemNames.OrderRefundedCustomerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.OrderTokens, TokenGroupNames.RefundedOrderTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.NewOrderNoteAddedCustomerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.OrderNoteTokens, TokenGroupNames.OrderTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.RecurringPaymentCancelledStoreOwnerNotification or
            MessageTemplateSystemNames.RecurringPaymentCancelledCustomerNotification or
            MessageTemplateSystemNames.RecurringPaymentFailedCustomerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.OrderTokens, TokenGroupNames.CustomerTokens, TokenGroupNames.RecurringPaymentTokens],

            MessageTemplateSystemNames.NewsletterSubscriptionActivationMessage or
            MessageTemplateSystemNames.NewsletterSubscriptionDeactivationMessage
                => [TokenGroupNames.StoreTokens, TokenGroupNames.SubscriptionTokens],

            MessageTemplateSystemNames.EmailAFriendMessage
                => [TokenGroupNames.StoreTokens, TokenGroupNames.CustomerTokens, TokenGroupNames.ProductTokens, TokenGroupNames.EmailAFriendTokens],

            MessageTemplateSystemNames.WishlistToFriendMessage
                => [TokenGroupNames.StoreTokens, TokenGroupNames.CustomerTokens, TokenGroupNames.WishlistToFriendTokens],

            MessageTemplateSystemNames.NewReturnRequestStoreOwnerNotification or
            MessageTemplateSystemNames.NewReturnRequestCustomerNotification or
            MessageTemplateSystemNames.ReturnRequestStatusChangedCustomerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.CustomerTokens, TokenGroupNames.ReturnRequestTokens],

            MessageTemplateSystemNames.NewForumTopicMessage
                => [TokenGroupNames.StoreTokens, TokenGroupNames.ForumTopicTokens, TokenGroupNames.ForumTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.NewForumPostMessage
                => [TokenGroupNames.StoreTokens, TokenGroupNames.ForumPostTokens, TokenGroupNames.ForumTopicTokens, TokenGroupNames.ForumTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.PrivateMessageNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.PrivateMessageTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.NewVendorAccountApplyStoreOwnerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.CustomerTokens, TokenGroupNames.VendorTokens],

            MessageTemplateSystemNames.VendorInformationChangeNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.VendorTokens],

            MessageTemplateSystemNames.GiftCardNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.GiftCardTokens],

            MessageTemplateSystemNames.ProductReviewNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.ProductReviewTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.QuantityBelowStoreOwnerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.ProductTokens],

            MessageTemplateSystemNames.QuantityBelowAttributeCombinationStoreOwnerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.ProductTokens, TokenGroupNames.AttributeCombinationTokens],

            MessageTemplateSystemNames.NewVatSubmittedStoreOwnerNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.CustomerTokens, TokenGroupNames.VatValidation],

            MessageTemplateSystemNames.BlogCommentNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.BlogCommentTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.NewsCommentNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.NewsCommentTokens, TokenGroupNames.CustomerTokens],

            MessageTemplateSystemNames.BackInStockNotification
                => [TokenGroupNames.StoreTokens, TokenGroupNames.CustomerTokens, TokenGroupNames.ProductBackInStockTokens],

            MessageTemplateSystemNames.ContactUsMessage
                => [TokenGroupNames.StoreTokens, TokenGroupNames.ContactUs],

            MessageTemplateSystemNames.ContactVendorMessage
                => [TokenGroupNames.StoreTokens, TokenGroupNames.ContactVendor],

            _ => []
        };
    }
}
