using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Messages;

public interface IWorkflowMessageService
{
    // Customer workflow
    Task<int> SendCustomerRegisteredNotificationMessageAsync(Customer customer, int languageId);
    Task<int> SendCustomerWelcomeMessageAsync(Customer customer, int languageId);
    Task<int> SendCustomerEmailValidationMessageAsync(Customer customer, int languageId);
    Task<int> SendCustomerEmailRevalidationMessageAsync(Customer customer, int languageId);
    Task<int> SendCustomerPasswordRecoveryMessageAsync(Customer customer, int languageId);

    // Order workflow
    Task<int> SendOrderPlacedVendorNotificationAsync(Order order, Vendor vendor, int languageId);
    Task<int> SendOrderPlacedStoreOwnerNotificationAsync(Order order, int languageId);
    Task<int> SendOrderPaidStoreOwnerNotificationAsync(Order order, int languageId);
    Task<int> SendOrderPaidCustomerNotificationAsync(Order order, int languageId, string? attachmentFilePath = null, string? attachmentFileName = null);
    Task<int> SendOrderPaidVendorNotificationAsync(Order order, Vendor vendor, int languageId);
    Task<int> SendOrderPlacedCustomerNotificationAsync(Order order, int languageId, string? attachmentFilePath = null, string? attachmentFileName = null);
    Task<int> SendShipmentSentCustomerNotificationAsync(Shipment shipment, int languageId);
    Task<int> SendShipmentDeliveredCustomerNotificationAsync(Shipment shipment, int languageId);
    Task<int> SendOrderCompletedCustomerNotificationAsync(Order order, int languageId, string? attachmentFilePath = null, string? attachmentFileName = null);
    Task<int> SendOrderCancelledCustomerNotificationAsync(Order order, int languageId);
    Task<int> SendOrderRefundedStoreOwnerNotificationAsync(Order order, decimal refundedAmount, int languageId);
    Task<int> SendOrderRefundedCustomerNotificationAsync(Order order, decimal refundedAmount, int languageId);
    Task<int> SendNewOrderNoteAddedCustomerNotificationAsync(OrderNote orderNote, int languageId);
    Task<int> SendRecurringPaymentCancelledStoreOwnerNotificationAsync(RecurringPayment recurringPayment, int languageId);
    Task<int> SendRecurringPaymentCancelledCustomerNotificationAsync(RecurringPayment recurringPayment, int languageId);
    Task<int> SendRecurringPaymentFailedCustomerNotificationAsync(RecurringPayment recurringPayment, int languageId);

    // Newsletter workflow
    Task<int> SendNewsLetterSubscriptionActivationMessageAsync(NewsLetterSubscription subscription, int languageId);
    Task<int> SendNewsLetterSubscriptionDeactivationMessageAsync(NewsLetterSubscription subscription, int languageId);

    // Send to friend
    Task<int> SendProductEmailAFriendMessageAsync(Customer customer, int languageId, Product product, string customerEmail, string friendsEmail, string personalMessage);
    Task<int> SendWishlistEmailAFriendMessageAsync(Customer customer, int languageId, string customerEmail, string friendsEmail, string personalMessage);

    // Return requests
    Task<int> SendNewReturnRequestStoreOwnerNotificationAsync(ReturnRequest returnRequest, OrderItem orderItem, int languageId);
    Task<int> SendNewReturnRequestCustomerNotificationAsync(ReturnRequest returnRequest, OrderItem orderItem, int languageId);
    Task<int> SendReturnRequestStatusChangedCustomerNotificationAsync(ReturnRequest returnRequest, OrderItem orderItem, int languageId);

    // Forum
    Task<int> SendNewForumTopicMessageAsync(Customer customer, ForumTopic forumTopic, Forum forum, int languageId);
    Task<int> SendNewForumPostMessageAsync(Customer customer, ForumPost forumPost, ForumTopic forumTopic, Forum forum, int friendlyForumTopicPageIndex, int languageId);
    Task<int> SendPrivateMessageNotificationAsync(PrivateMessage privateMessage, int languageId);

    // Misc
    Task<int> SendNewVendorAccountApplyStoreOwnerNotificationAsync(Customer customer, Vendor vendor, int languageId);
    Task<int> SendVendorInformationChangeNotificationAsync(Vendor vendor, int languageId);
    Task<int> SendProductReviewNotificationMessageAsync(ProductReview productReview, int languageId);
    Task<int> SendGiftCardNotificationAsync(GiftCard giftCard, int languageId);
    Task<int> SendQuantityBelowStoreOwnerNotificationAsync(Product product, int languageId);
    Task<int> SendQuantityBelowStoreOwnerNotificationAsync(ProductAttributeCombination combination, int languageId);
    Task<int> SendNewVatSubmittedStoreOwnerNotificationAsync(Customer customer, string vatName, string vatAddress, int languageId);
    Task<int> SendBlogCommentNotificationMessageAsync(BlogComment blogComment, int languageId);
    Task<int> SendNewsCommentNotificationMessageAsync(NewsComment newsComment, int languageId);
    Task<int> SendBackInStockNotificationAsync(BackInStockSubscription subscription, int languageId);
    Task<int> SendContactUsMessageAsync(int languageId, string senderEmail, string senderName, string? subject, string body);
    Task<int> SendContactVendorMessageAsync(Vendor vendor, int languageId, string senderEmail, string senderName, string? subject, string body);
    Task<int> SendTestEmailAsync(int messageTemplateId, string sendToEmail, List<Token> tokens, int languageId);

    Task<int> SendNotificationAsync(MessageTemplate messageTemplate, EmailAccount emailAccount,
        int languageId, IEnumerable<Token> tokens, string toEmailAddress, string toName,
        string? attachmentFilePath = null, string? attachmentFileName = null,
        string? replyToEmailAddress = null, string? replyToName = null,
        string? fromEmail = null, string? fromName = null, string? subject = null);
}
