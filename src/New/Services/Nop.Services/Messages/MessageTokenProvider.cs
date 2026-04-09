using System.Net;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Vendors;
using Nop.Services.Events;
using Nop.Services.Stores;

namespace Nop.Services.Messages;

public class MessageTokenProvider(
    IStoreContext storeContext,
    IRepository<GenericAttribute> genericAttributeRepository,
    IEventPublisher eventPublisher) : IMessageTokenProvider
{
    private static readonly Dictionary<string, IEnumerable<string>> AllowedTokens = BuildAllowedTokens();

    public Task AddStoreTokensAsync(IList<Token> tokens, Store store, EmailAccount emailAccount)
    {
        tokens.Add(new Token("Store.Name", store.Name ?? string.Empty));
        tokens.Add(new Token("Store.URL", store.Url ?? string.Empty, neverHtmlEncoded: true));
        tokens.Add(new Token("Store.Email", emailAccount.Email ?? string.Empty));
        tokens.Add(new Token("Store.CompanyName", store.CompanyName ?? string.Empty));
        tokens.Add(new Token("Store.CompanyAddress", store.CompanyAddress ?? string.Empty));
        tokens.Add(new Token("Store.CompanyPhoneNumber", store.CompanyPhoneNumber ?? string.Empty));
        tokens.Add(new Token("Store.CompanyVat", store.CompanyVat ?? string.Empty));

        tokens.Add(new Token("Facebook.URL", GetStoreUrl(store) + "facebook", neverHtmlEncoded: true));
        tokens.Add(new Token("Twitter.URL", GetStoreUrl(store) + "twitter", neverHtmlEncoded: true));
        tokens.Add(new Token("YouTube.URL", GetStoreUrl(store) + "youtube", neverHtmlEncoded: true));
        tokens.Add(new Token("GooglePlus.URL", string.Empty, neverHtmlEncoded: true));

        eventPublisher.PublishAsync(new EntityTokensAddedEvent<Store, Token>(store, tokens));
        return Task.CompletedTask;
    }

    public Task AddCustomerTokensAsync(IList<Token> tokens, Customer customer)
    {
        tokens.Add(new Token("Customer.Email", customer.Email ?? string.Empty));
        tokens.Add(new Token("Customer.Username", customer.Username ?? string.Empty));
        tokens.Add(new Token("Customer.FullName", GetCustomerFullName(customer)));
        tokens.Add(new Token("Customer.FirstName", GetGenericAttribute(customer.Id, "Customer", "FirstName")));
        tokens.Add(new Token("Customer.LastName", GetGenericAttribute(customer.Id, "Customer", "LastName")));
        tokens.Add(new Token("Customer.VatNumber", GetGenericAttribute(customer.Id, "Customer", "VatNumber")));
        tokens.Add(new Token("Customer.VatNumberStatus", string.Empty));
        tokens.Add(new Token("Customer.CustomAttributes", string.Empty));

        // URL tokens — store URL + path
        var store = storeContext.CurrentStore;
        var storeUrl = GetStoreUrl(store);
        tokens.Add(new Token("Customer.PasswordRecoveryURL",
            $"{storeUrl}passwordrecovery/confirm?token={GetGenericAttribute(customer.Id, "Customer", "PasswordRecoveryToken")}&email={WebUtility.UrlEncode(customer.Email)}",
            neverHtmlEncoded: true));
        tokens.Add(new Token("Customer.AccountActivationURL",
            $"{storeUrl}customer/activation?token={GetGenericAttribute(customer.Id, "Customer", "AccountActivationToken")}&email={WebUtility.UrlEncode(customer.Email)}",
            neverHtmlEncoded: true));
        tokens.Add(new Token("Customer.EmailRevalidationURL",
            $"{storeUrl}customer/revalidateemail?token={GetGenericAttribute(customer.Id, "Customer", "EmailRevalidationToken")}&email={WebUtility.UrlEncode(customer.Email)}",
            neverHtmlEncoded: true));
        tokens.Add(new Token("Wishlist.URLForCustomer",
            $"{storeUrl}wishlist/{customer.CustomerGuid}",
            neverHtmlEncoded: true));

        eventPublisher.PublishAsync(new EntityTokensAddedEvent<Customer, Token>(customer, tokens));
        return Task.CompletedTask;
    }

    public Task AddVendorTokensAsync(IList<Token> tokens, Vendor vendor)
    {
        tokens.Add(new Token("Vendor.Name", vendor.Name ?? string.Empty));
        tokens.Add(new Token("Vendor.Email", vendor.Email ?? string.Empty));

        eventPublisher.PublishAsync(new EntityTokensAddedEvent<Vendor, Token>(vendor, tokens));
        return Task.CompletedTask;
    }

    public Task AddNewsLetterSubscriptionTokensAsync(IList<Token> tokens, NewsLetterSubscription subscription)
    {
        tokens.Add(new Token("NewsLetterSubscription.Email", subscription.Email ?? string.Empty));

        var store = storeContext.CurrentStore;
        var storeUrl = GetStoreUrl(store);
        tokens.Add(new Token("NewsLetterSubscription.ActivationUrl",
            $"{storeUrl}newsletter/subscriptionactivation/{subscription.NewsLetterSubscriptionGuid}/true",
            neverHtmlEncoded: true));
        tokens.Add(new Token("NewsLetterSubscription.DeactivationUrl",
            $"{storeUrl}newsletter/subscriptionactivation/{subscription.NewsLetterSubscriptionGuid}/false",
            neverHtmlEncoded: true));

        eventPublisher.PublishAsync(new EntityTokensAddedEvent<NewsLetterSubscription, Token>(subscription, tokens));
        return Task.CompletedTask;
    }

    public Task AddBlogCommentTokensAsync(IList<Token> tokens, BlogComment blogComment)
    {
        var store = storeContext.CurrentStore;
        tokens.Add(new Token("BlogComment.BlogPostTitle", string.Empty)); // requires blog post lookup
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<BlogComment, Token>(blogComment, tokens));
        return Task.CompletedTask;
    }

    public Task AddNewsCommentTokensAsync(IList<Token> tokens, NewsComment newsComment)
    {
        var store = storeContext.CurrentStore;
        tokens.Add(new Token("NewsComment.NewsTitle", string.Empty)); // requires news item lookup
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<NewsComment, Token>(newsComment, tokens));
        return Task.CompletedTask;
    }

    public Task AddProductTokensAsync(IList<Token> tokens, Product product, int languageId)
    {
        var store = storeContext.CurrentStore;
        var storeUrl = GetStoreUrl(store);

        tokens.Add(new Token("Product.ID", product.Id));
        tokens.Add(new Token("Product.Name", product.Name ?? string.Empty));
        tokens.Add(new Token("Product.ShortDescription", product.ShortDescription ?? string.Empty));
        tokens.Add(new Token("Product.ProductURLForCustomer", $"{storeUrl}product/{product.Id}", neverHtmlEncoded: true));
        tokens.Add(new Token("Product.SKU", product.Sku ?? string.Empty));
        tokens.Add(new Token("Product.StockQuantity", product.StockQuantity));

        eventPublisher.PublishAsync(new EntityTokensAddedEvent<Product, Token>(product, tokens));
        return Task.CompletedTask;
    }

    public Task AddProductReviewTokensAsync(IList<Token> tokens, ProductReview productReview)
    {
        tokens.Add(new Token("ProductReview.ProductName", string.Empty)); // requires product lookup
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<ProductReview, Token>(productReview, tokens));
        return Task.CompletedTask;
    }

    // Order-related tokens — minimal implementation, will be enriched when IOrderService is built [4.9]
    public Task AddOrderTokensAsync(IList<Token> tokens, Order order, int languageId, int vendorId = 0)
    {
        tokens.Add(new Token("Order.OrderNumber", order.CustomOrderNumber ?? order.Id.ToString()));
        tokens.Add(new Token("Order.CustomerFullName", string.Empty));
        tokens.Add(new Token("Order.CustomerEmail", string.Empty));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<Order, Token>(order, tokens));
        return Task.CompletedTask;
    }

    public Task AddOrderRefundedTokensAsync(IList<Token> tokens, Order order, decimal refundedAmount)
    {
        tokens.Add(new Token("Order.AmountRefunded", refundedAmount.ToString("F2")));
        return Task.CompletedTask;
    }

    public Task AddShipmentTokensAsync(IList<Token> tokens, Shipment shipment, int languageId)
    {
        tokens.Add(new Token("Shipment.ShipmentNumber", shipment.Id));
        tokens.Add(new Token("Shipment.TrackingNumber", shipment.TrackingNumber ?? string.Empty));
        tokens.Add(new Token("Shipment.TrackingNumberURL", string.Empty, neverHtmlEncoded: true));
        tokens.Add(new Token("Shipment.Product(s)", string.Empty, neverHtmlEncoded: true));
        tokens.Add(new Token("Shipment.URLForCustomer", string.Empty, neverHtmlEncoded: true));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<Shipment, Token>(shipment, tokens));
        return Task.CompletedTask;
    }

    public Task AddOrderNoteTokensAsync(IList<Token> tokens, OrderNote orderNote)
    {
        tokens.Add(new Token("Order.NewNoteText", orderNote.Note ?? string.Empty, neverHtmlEncoded: true));
        tokens.Add(new Token("Order.OrderNoteAttachmentUrl", string.Empty, neverHtmlEncoded: true));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<OrderNote, Token>(orderNote, tokens));
        return Task.CompletedTask;
    }

    public Task AddRecurringPaymentTokensAsync(IList<Token> tokens, RecurringPayment recurringPayment)
    {
        tokens.Add(new Token("RecurringPayment.ID", recurringPayment.Id));
        tokens.Add(new Token("RecurringPayment.CancelAfterFailedPayment", recurringPayment.LastPaymentFailed));
        tokens.Add(new Token("RecurringPayment.RecurringPaymentType", string.Empty));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<RecurringPayment, Token>(recurringPayment, tokens));
        return Task.CompletedTask;
    }

    public Task AddReturnRequestTokensAsync(IList<Token> tokens, ReturnRequest returnRequest, OrderItem orderItem)
    {
        tokens.Add(new Token("ReturnRequest.CustomNumber", returnRequest.CustomNumber ?? returnRequest.Id.ToString()));
        tokens.Add(new Token("ReturnRequest.OrderId", returnRequest.OrderItemId));
        tokens.Add(new Token("ReturnRequest.Product.Quantity", returnRequest.Quantity));
        tokens.Add(new Token("ReturnRequest.Product.Name", string.Empty));
        tokens.Add(new Token("ReturnRequest.Reason", returnRequest.ReasonForReturn ?? string.Empty));
        tokens.Add(new Token("ReturnRequest.RequestedAction", returnRequest.RequestedAction ?? string.Empty));
        tokens.Add(new Token("ReturnRequest.CustomerComment", returnRequest.CustomerComments ?? string.Empty, neverHtmlEncoded: true));
        tokens.Add(new Token("ReturnRequest.StaffNotes", returnRequest.StaffNotes ?? string.Empty, neverHtmlEncoded: true));
        tokens.Add(new Token("ReturnRequest.Status", string.Empty));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<ReturnRequest, Token>(returnRequest, tokens));
        return Task.CompletedTask;
    }

    public Task AddGiftCardTokensAsync(IList<Token> tokens, GiftCard giftCard)
    {
        tokens.Add(new Token("GiftCard.SenderName", giftCard.SenderName ?? string.Empty));
        tokens.Add(new Token("GiftCard.SenderEmail", giftCard.SenderEmail ?? string.Empty));
        tokens.Add(new Token("GiftCard.RecipientName", giftCard.RecipientName ?? string.Empty));
        tokens.Add(new Token("GiftCard.RecipientEmail", giftCard.RecipientEmail ?? string.Empty));
        tokens.Add(new Token("GiftCard.Amount", giftCard.Amount.ToString("F2")));
        tokens.Add(new Token("GiftCard.CouponCode", giftCard.GiftCardCouponCode ?? string.Empty));
        tokens.Add(new Token("GiftCard.Message", giftCard.Message ?? string.Empty, neverHtmlEncoded: true));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<GiftCard, Token>(giftCard, tokens));
        return Task.CompletedTask;
    }

    public Task AddAttributeCombinationTokensAsync(IList<Token> tokens, ProductAttributeCombination combination, int languageId)
    {
        tokens.Add(new Token("AttributeCombination.Formatted", string.Empty));
        tokens.Add(new Token("AttributeCombination.SKU", combination.Sku ?? string.Empty));
        tokens.Add(new Token("AttributeCombination.StockQuantity", combination.StockQuantity));
        return Task.CompletedTask;
    }

    public Task AddForumTokensAsync(IList<Token> tokens, Forum forum)
    {
        var store = storeContext.CurrentStore;
        var storeUrl = GetStoreUrl(store);
        tokens.Add(new Token("Forums.ForumURL", $"{storeUrl}boards/forum/{forum.Id}", neverHtmlEncoded: true));
        tokens.Add(new Token("Forums.ForumName", forum.Name ?? string.Empty));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<Forum, Token>(forum, tokens));
        return Task.CompletedTask;
    }

    public Task AddForumTopicTokensAsync(IList<Token> tokens, ForumTopic forumTopic, int? friendlyForumTopicPageIndex = null, int? appendedPostIdentifierAnchor = null)
    {
        var store = storeContext.CurrentStore;
        var storeUrl = GetStoreUrl(store);
        var url = $"{storeUrl}boards/topic/{forumTopic.Id}";
        if (friendlyForumTopicPageIndex.HasValue && friendlyForumTopicPageIndex.Value > 1)
            url += $"/{friendlyForumTopicPageIndex.Value}";
        if (appendedPostIdentifierAnchor.HasValue)
            url += $"#{appendedPostIdentifierAnchor.Value}";

        tokens.Add(new Token("Forums.TopicURL", url, neverHtmlEncoded: true));
        tokens.Add(new Token("Forums.TopicName", forumTopic.Subject ?? string.Empty));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<ForumTopic, Token>(forumTopic, tokens));
        return Task.CompletedTask;
    }

    public Task AddForumPostTokensAsync(IList<Token> tokens, ForumPost forumPost)
    {
        tokens.Add(new Token("Forums.PostAuthor", string.Empty)); // requires customer lookup
        tokens.Add(new Token("Forums.PostBody", forumPost.Text ?? string.Empty, neverHtmlEncoded: true));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<ForumPost, Token>(forumPost, tokens));
        return Task.CompletedTask;
    }

    public Task AddPrivateMessageTokensAsync(IList<Token> tokens, PrivateMessage privateMessage)
    {
        var store = storeContext.CurrentStore;
        var storeUrl = GetStoreUrl(store);
        tokens.Add(new Token("PrivateMessage.Subject", privateMessage.Subject ?? string.Empty));
        tokens.Add(new Token("PrivateMessage.Text", privateMessage.Text ?? string.Empty, neverHtmlEncoded: true));
        tokens.Add(new Token("PrivateMessage.URL", $"{storeUrl}privatemessages/view/{privateMessage.Id}", neverHtmlEncoded: true));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<PrivateMessage, Token>(privateMessage, tokens));
        return Task.CompletedTask;
    }

    public Task AddBackInStockTokensAsync(IList<Token> tokens, BackInStockSubscription subscription)
    {
        tokens.Add(new Token("BackInStockSubscription.ProductName", string.Empty)); // requires product lookup
        tokens.Add(new Token("BackInStockSubscription.ProductUrl", string.Empty, neverHtmlEncoded: true));
        eventPublisher.PublishAsync(new EntityTokensAddedEvent<BackInStockSubscription, Token>(subscription, tokens));
        return Task.CompletedTask;
    }

    public IEnumerable<string> GetListOfCampaignAllowedTokens()
    {
        return GetListOfAllowedTokens([
            TokenGroupNames.StoreTokens,
            TokenGroupNames.SubscriptionTokens
        ]);
    }

    public IEnumerable<string> GetListOfAllowedTokens(IEnumerable<string>? tokenGroups = null)
    {
        var groups = tokenGroups?.ToList() ?? AllowedTokens.Keys.ToList();
        return groups.SelectMany(g => AllowedTokens.TryGetValue(g, out var t) ? t : []).Distinct();
    }

    #region Helpers

    private static string GetStoreUrl(Store store)
    {
        var url = store.Url ?? string.Empty;
        if (!url.EndsWith('/'))
            url += '/';
        return url;
    }

    private string GetCustomerFullName(Customer customer)
    {
        var first = GetGenericAttribute(customer.Id, "Customer", "FirstName");
        var last = GetGenericAttribute(customer.Id, "Customer", "LastName");
        return $"{first} {last}".Trim();
    }

    private string GetGenericAttribute(int entityId, string keyGroup, string key)
    {
        return genericAttributeRepository.TableNoTracking
            .Where(ga => ga.EntityId == entityId && ga.KeyGroup == keyGroup && ga.Key == key)
            .Select(ga => ga.Value)
            .FirstOrDefault() ?? string.Empty;
    }

    private static Dictionary<string, IEnumerable<string>> BuildAllowedTokens()
    {
        return new Dictionary<string, IEnumerable<string>>
        {
            [TokenGroupNames.StoreTokens] = ["%Store.Name%", "%Store.URL%", "%Store.Email%", "%Store.CompanyName%", "%Store.CompanyAddress%", "%Store.CompanyPhoneNumber%", "%Store.CompanyVat%", "%Facebook.URL%", "%Twitter.URL%", "%YouTube.URL%", "%GooglePlus.URL%"],
            [TokenGroupNames.CustomerTokens] = ["%Customer.Email%", "%Customer.Username%", "%Customer.FullName%", "%Customer.FirstName%", "%Customer.LastName%", "%Customer.VatNumber%", "%Customer.VatNumberStatus%", "%Customer.CustomAttributes%", "%Customer.PasswordRecoveryURL%", "%Customer.AccountActivationURL%", "%Customer.EmailRevalidationURL%", "%Wishlist.URLForCustomer%"],
            [TokenGroupNames.OrderTokens] = ["%Order.OrderNumber%", "%Order.CustomerFullName%", "%Order.CustomerEmail%", "%Order.BillingFirstName%", "%Order.BillingLastName%", "%Order.BillingPhoneNumber%", "%Order.BillingEmail%", "%Order.BillingFaxNumber%", "%Order.BillingCompany%", "%Order.BillingAddress1%", "%Order.BillingAddress2%", "%Order.BillingCity%", "%Order.BillingStateProvince%", "%Order.BillingZipPostalCode%", "%Order.BillingCountry%", "%Order.BillingCustomAttributes%", "%Order.Shippable%", "%Order.ShippingMethod%", "%Order.ShippingFirstName%", "%Order.ShippingLastName%", "%Order.ShippingPhoneNumber%", "%Order.ShippingEmail%", "%Order.ShippingFaxNumber%", "%Order.ShippingCompany%", "%Order.ShippingAddress1%", "%Order.ShippingAddress2%", "%Order.ShippingCity%", "%Order.ShippingStateProvince%", "%Order.ShippingZipPostalCode%", "%Order.ShippingCountry%", "%Order.ShippingCustomAttributes%", "%Order.PaymentMethod%", "%Order.VatNumber%", "%Order.CustomValues%", "%Order.Product(s)%", "%Order.CreatedOn%", "%Order.OrderURLForCustomer%"],
            [TokenGroupNames.ShipmentTokens] = ["%Shipment.ShipmentNumber%", "%Shipment.TrackingNumber%", "%Shipment.TrackingNumberURL%", "%Shipment.Product(s)%", "%Shipment.URLForCustomer%"],
            [TokenGroupNames.RefundedOrderTokens] = ["%Order.AmountRefunded%"],
            [TokenGroupNames.OrderNoteTokens] = ["%Order.NewNoteText%", "%Order.OrderNoteAttachmentUrl%"],
            [TokenGroupNames.RecurringPaymentTokens] = ["%RecurringPayment.ID%", "%RecurringPayment.CancelAfterFailedPayment%", "%RecurringPayment.RecurringPaymentType%"],
            [TokenGroupNames.SubscriptionTokens] = ["%NewsLetterSubscription.Email%", "%NewsLetterSubscription.ActivationUrl%", "%NewsLetterSubscription.DeactivationUrl%"],
            [TokenGroupNames.ProductTokens] = ["%Product.ID%", "%Product.Name%", "%Product.ShortDescription%", "%Product.ProductURLForCustomer%", "%Product.SKU%", "%Product.StockQuantity%"],
            [TokenGroupNames.ReturnRequestTokens] = ["%ReturnRequest.CustomNumber%", "%ReturnRequest.OrderId%", "%ReturnRequest.Product.Quantity%", "%ReturnRequest.Product.Name%", "%ReturnRequest.Reason%", "%ReturnRequest.RequestedAction%", "%ReturnRequest.CustomerComment%", "%ReturnRequest.StaffNotes%", "%ReturnRequest.Status%"],
            [TokenGroupNames.ForumTokens] = ["%Forums.ForumURL%", "%Forums.ForumName%"],
            [TokenGroupNames.ForumTopicTokens] = ["%Forums.TopicURL%", "%Forums.TopicName%"],
            [TokenGroupNames.ForumPostTokens] = ["%Forums.PostAuthor%", "%Forums.PostBody%"],
            [TokenGroupNames.PrivateMessageTokens] = ["%PrivateMessage.Subject%", "%PrivateMessage.Text%", "%PrivateMessage.URL%"],
            [TokenGroupNames.VendorTokens] = ["%Vendor.Name%", "%Vendor.Email%"],
            [TokenGroupNames.GiftCardTokens] = ["%GiftCard.SenderName%", "%GiftCard.SenderEmail%", "%GiftCard.RecipientName%", "%GiftCard.RecipientEmail%", "%GiftCard.Amount%", "%GiftCard.CouponCode%", "%GiftCard.Message%"],
            [TokenGroupNames.ProductReviewTokens] = ["%ProductReview.ProductName%"],
            [TokenGroupNames.AttributeCombinationTokens] = ["%AttributeCombination.Formatted%", "%AttributeCombination.SKU%", "%AttributeCombination.StockQuantity%"],
            [TokenGroupNames.BlogCommentTokens] = ["%BlogComment.BlogPostTitle%"],
            [TokenGroupNames.NewsCommentTokens] = ["%NewsComment.NewsTitle%"],
            [TokenGroupNames.ProductBackInStockTokens] = ["%BackInStockSubscription.ProductName%", "%BackInStockSubscription.ProductUrl%"],
            [TokenGroupNames.EmailAFriendTokens] = ["%EmailAFriend.PersonalMessage%", "%EmailAFriend.Email%"],
            [TokenGroupNames.WishlistToFriendTokens] = ["%Wishlist.PersonalMessage%", "%Wishlist.Email%", "%Wishlist.URLForCustomer%"],
            [TokenGroupNames.VatValidation] = ["%VatValidationResult.Name%", "%VatValidationResult.Address%"],
            [TokenGroupNames.ContactUs] = ["%ContactUs.SenderEmail%", "%ContactUs.SenderName%", "%ContactUs.Body%"],
            [TokenGroupNames.ContactVendor] = ["%ContactUs.SenderEmail%", "%ContactUs.SenderName%", "%ContactUs.Body%"]
        };
    }

    #endregion
}

/// <summary>
/// Published when entity-specific tokens are added to a token list.
/// </summary>
public class EntityTokensAddedEvent<T, U>(T entity, IList<U> tokens) where T : BaseEntity
{
    public T Entity { get; } = entity;
    public IList<U> Tokens { get; } = tokens;
}

/// <summary>
/// Published when message template tokens are added.
/// </summary>
public class MessageTokensAddedEvent<U>(MessageTemplate messageTemplate, IList<U> tokens)
{
    public MessageTemplate MessageTemplate { get; } = messageTemplate;
    public IList<U> Tokens { get; } = tokens;
}
