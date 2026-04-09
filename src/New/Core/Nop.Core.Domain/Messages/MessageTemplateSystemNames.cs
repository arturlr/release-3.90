namespace Nop.Core.Domain.Messages
{

    public static partial class MessageTemplateSystemNames
    {
        #region Customer

        public const string CustomerRegisteredNotification = "NewCustomer.Notification";

        public const string CustomerWelcomeMessage = "Customer.WelcomeMessage";

        public const string CustomerEmailValidationMessage = "Customer.EmailValidationMessage";

        public const string CustomerEmailRevalidationMessage = "Customer.EmailRevalidationMessage";

        public const string CustomerPasswordRecoveryMessage = "Customer.PasswordRecovery";

        #endregion

        #region Order

        public const string OrderPlacedVendorNotification = "OrderPlaced.VendorNotification";

        public const string OrderPlacedStoreOwnerNotification = "OrderPlaced.StoreOwnerNotification";

        public const string OrderPaidStoreOwnerNotification = "OrderPaid.StoreOwnerNotification";

        public const string OrderPaidCustomerNotification = "OrderPaid.CustomerNotification";

        public const string OrderPaidVendorNotification = "OrderPaid.VendorNotification";

        public const string OrderPlacedCustomerNotification = "OrderPlaced.CustomerNotification";

        public const string ShipmentSentCustomerNotification = "ShipmentSent.CustomerNotification";

        public const string ShipmentDeliveredCustomerNotification = "ShipmentDelivered.CustomerNotification";

        public const string OrderCompletedCustomerNotification = "OrderCompleted.CustomerNotification";

        public const string OrderCancelledCustomerNotification = "OrderCancelled.CustomerNotification";

        public const string OrderRefundedStoreOwnerNotification = "OrderRefunded.StoreOwnerNotification";

        public const string OrderRefundedCustomerNotification = "OrderRefunded.CustomerNotification";

        public const string NewOrderNoteAddedCustomerNotification = "Customer.NewOrderNote";

        public const string RecurringPaymentCancelledStoreOwnerNotification = "RecurringPaymentCancelled.StoreOwnerNotification";

        public const string RecurringPaymentCancelledCustomerNotification = "RecurringPaymentCancelled.CustomerNotification";

        public const string RecurringPaymentFailedCustomerNotification = "RecurringPaymentFailed.CustomerNotification";

        #endregion

        #region Newsletter

        public const string NewsletterSubscriptionActivationMessage = "NewsLetterSubscription.ActivationMessage";

        public const string NewsletterSubscriptionDeactivationMessage = "NewsLetterSubscription.DeactivationMessage";

        #endregion

        #region To friend

        public const string EmailAFriendMessage = "Service.EmailAFriend";

        public const string WishlistToFriendMessage = "Wishlist.EmailAFriend";

        #endregion

        #region Return requests

        public const string NewReturnRequestStoreOwnerNotification = "NewReturnRequest.StoreOwnerNotification";

        public const string NewReturnRequestCustomerNotification = "NewReturnRequest.CustomerNotification";

        public const string ReturnRequestStatusChangedCustomerNotification = "ReturnRequestStatusChanged.CustomerNotification";

        #endregion

        #region Forum

        public const string NewForumTopicMessage = "Forums.NewForumTopic";

        public const string NewForumPostMessage = "Forums.NewForumPost";

        public const string PrivateMessageNotification = "Customer.NewPM";

        #endregion

        #region Misc

        public const string NewVendorAccountApplyStoreOwnerNotification = "VendorAccountApply.StoreOwnerNotification";

        public const string VendorInformationChangeNotification = "VendorInformationChange.StoreOwnerNotification";

        public const string GiftCardNotification = "GiftCard.Notification";

        public const string ProductReviewNotification = "Product.ProductReview";

        public const string QuantityBelowStoreOwnerNotification = "QuantityBelow.StoreOwnerNotification";

        public const string QuantityBelowAttributeCombinationStoreOwnerNotification = "QuantityBelow.AttributeCombination.StoreOwnerNotification";

        public const string NewVatSubmittedStoreOwnerNotification = "NewVATSubmitted.StoreOwnerNotification";

        public const string BlogCommentNotification = "Blog.BlogComment";

        public const string NewsCommentNotification = "News.NewsComment";

        public const string BackInStockNotification = "Customer.BackInStock";

        public const string ContactUsMessage = "Service.ContactUs";

        public const string ContactVendorMessage = "Service.ContactVendor";

        #endregion
    }
}
