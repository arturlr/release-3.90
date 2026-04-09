using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Messages;

public interface IMessageTokenProvider
{
    Task AddStoreTokensAsync(IList<Token> tokens, Store store, EmailAccount emailAccount);
    Task AddCustomerTokensAsync(IList<Token> tokens, Customer customer);
    Task AddOrderTokensAsync(IList<Token> tokens, Order order, int languageId, int vendorId = 0);
    Task AddOrderRefundedTokensAsync(IList<Token> tokens, Order order, decimal refundedAmount);
    Task AddShipmentTokensAsync(IList<Token> tokens, Shipment shipment, int languageId);
    Task AddOrderNoteTokensAsync(IList<Token> tokens, OrderNote orderNote);
    Task AddRecurringPaymentTokensAsync(IList<Token> tokens, RecurringPayment recurringPayment);
    Task AddReturnRequestTokensAsync(IList<Token> tokens, ReturnRequest returnRequest, OrderItem orderItem);
    Task AddGiftCardTokensAsync(IList<Token> tokens, GiftCard giftCard);
    Task AddVendorTokensAsync(IList<Token> tokens, Vendor vendor);
    Task AddNewsLetterSubscriptionTokensAsync(IList<Token> tokens, NewsLetterSubscription subscription);
    Task AddProductReviewTokensAsync(IList<Token> tokens, ProductReview productReview);
    Task AddBlogCommentTokensAsync(IList<Token> tokens, BlogComment blogComment);
    Task AddNewsCommentTokensAsync(IList<Token> tokens, NewsComment newsComment);
    Task AddProductTokensAsync(IList<Token> tokens, Product product, int languageId);
    Task AddAttributeCombinationTokensAsync(IList<Token> tokens, ProductAttributeCombination combination, int languageId);
    Task AddForumTokensAsync(IList<Token> tokens, Forum forum);
    Task AddForumTopicTokensAsync(IList<Token> tokens, ForumTopic forumTopic, int? friendlyForumTopicPageIndex = null, int? appendedPostIdentifierAnchor = null);
    Task AddForumPostTokensAsync(IList<Token> tokens, ForumPost forumPost);
    Task AddPrivateMessageTokensAsync(IList<Token> tokens, PrivateMessage privateMessage);
    Task AddBackInStockTokensAsync(IList<Token> tokens, BackInStockSubscription subscription);
    IEnumerable<string> GetListOfCampaignAllowedTokens();
    IEnumerable<string> GetListOfAllowedTokens(IEnumerable<string>? tokenGroups = null);
}
