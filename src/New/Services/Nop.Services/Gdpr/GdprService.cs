using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Gdpr;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;

namespace Nop.Services.Gdpr;

public class GdprService : IGdprService
{
    private readonly IRepository<GdprLog> _gdprLogRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<CustomerPassword> _customerPasswordRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IRepository<GenericAttribute> _genericAttributeRepository;
    private readonly IRepository<Address> _addressRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<BlogComment> _blogCommentRepository;
    private readonly IRepository<NewsComment> _newsCommentRepository;
    private readonly IRepository<ForumPost> _forumPostRepository;
    private readonly IRepository<ForumTopic> _forumTopicRepository;
    private readonly IRepository<ForumSubscription> _forumSubscriptionRepository;
    private readonly IRepository<PrivateMessage> _privateMessageRepository;
    private readonly IRepository<ProductReview> _productReviewRepository;
    private readonly IRepository<ProductReviewHelpfulness> _productReviewHelpfulnessRepository;
    private readonly IRepository<ActivityLog> _activityLogRepository;
    private readonly IRepository<Log> _logRepository;
    private readonly IRepository<ShoppingCartItem> _shoppingCartItemRepository;
    private readonly IRepository<BackInStockSubscription> _backInStockSubscriptionRepository;
    private readonly IEventPublisher _eventPublisher;

    public GdprService(
        IRepository<GdprLog> gdprLogRepository,
        IRepository<Customer> customerRepository,
        IRepository<CustomerPassword> customerPasswordRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IRepository<GenericAttribute> genericAttributeRepository,
        IRepository<Address> addressRepository,
        IRepository<Order> orderRepository,
        IRepository<BlogComment> blogCommentRepository,
        IRepository<NewsComment> newsCommentRepository,
        IRepository<ForumPost> forumPostRepository,
        IRepository<ForumTopic> forumTopicRepository,
        IRepository<ForumSubscription> forumSubscriptionRepository,
        IRepository<PrivateMessage> privateMessageRepository,
        IRepository<ProductReview> productReviewRepository,
        IRepository<ProductReviewHelpfulness> productReviewHelpfulnessRepository,
        IRepository<ActivityLog> activityLogRepository,
        IRepository<Log> logRepository,
        IRepository<ShoppingCartItem> shoppingCartItemRepository,
        IRepository<BackInStockSubscription> backInStockSubscriptionRepository,
        IEventPublisher eventPublisher)
    {
        _gdprLogRepository = gdprLogRepository;
        _customerRepository = customerRepository;
        _customerPasswordRepository = customerPasswordRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _genericAttributeRepository = genericAttributeRepository;
        _addressRepository = addressRepository;
        _orderRepository = orderRepository;
        _blogCommentRepository = blogCommentRepository;
        _newsCommentRepository = newsCommentRepository;
        _forumPostRepository = forumPostRepository;
        _forumTopicRepository = forumTopicRepository;
        _forumSubscriptionRepository = forumSubscriptionRepository;
        _privateMessageRepository = privateMessageRepository;
        _productReviewRepository = productReviewRepository;
        _productReviewHelpfulnessRepository = productReviewHelpfulnessRepository;
        _activityLogRepository = activityLogRepository;
        _logRepository = logRepository;
        _shoppingCartItemRepository = shoppingCartItemRepository;
        _backInStockSubscriptionRepository = backInStockSubscriptionRepository;
        _eventPublisher = eventPublisher;
    }

    #region GDPR log

    public Task<GdprLog?> GetLogByIdAsync(int logId)
    {
        return Task.FromResult(logId == 0 ? null : _gdprLogRepository.GetById(logId));
    }

    public Task<IPagedList<GdprLog>> GetAllLogAsync(
        int customerId = 0, int requestTypeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _gdprLogRepository.Table;

        if (customerId > 0)
            query = query.Where(gl => gl.CustomerId == customerId);
        if (requestTypeId > 0)
            query = query.Where(gl => gl.RequestTypeId == requestTypeId);

        query = query.OrderByDescending(gl => gl.CreatedOnUtc);

        IPagedList<GdprLog> result = new PagedList<GdprLog>(query, pageIndex, pageSize);
        return Task.FromResult(result);
    }

    public async Task InsertLogAsync(Customer customer, GdprRequestType requestType, string? requestDetails)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var gdprLog = new GdprLog
        {
            CustomerId = customer.Id,
            RequestType = requestType,
            RequestDetails = requestDetails,
            CreatedOnUtc = DateTime.UtcNow
        };

        _gdprLogRepository.Insert(gdprLog);
        await _eventPublisher.EntityInsertedAsync(gdprLog);
    }

    public async Task DeleteLogAsync(GdprLog gdprLog)
    {
        ArgumentNullException.ThrowIfNull(gdprLog);
        _gdprLogRepository.Delete(gdprLog);
        await _eventPublisher.EntityDeletedAsync(gdprLog);
    }

    #endregion

    #region Customer deletion / anonymization

    public async Task PermanentDeleteCustomerAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        // Delete related data that doesn't need to be preserved
        DeleteForumContent(customer.Id);
        DeleteBlogComments(customer.Id);
        DeleteNewsComments(customer.Id);
        DeleteProductReviews(customer.Id);
        DeleteActivityLogs(customer.Id);
        DeleteSystemLogs(customer.Id);
        DeleteShoppingCartItems(customer.Id);
        DeleteBackInStockSubscriptions(customer.Id);
        DeletePrivateMessages(customer.Id);
        DeleteForumSubscriptions(customer.Id);
        DeleteGenericAttributes(customer.Id);
        DeleteCustomerPasswords(customer.Id);
        DeleteCustomerRoleMappings(customer.Id);

        // Anonymize addresses referenced by orders (preserve order history)
        AnonymizeOrderAddresses(customer.Id);

        // Anonymize the customer record itself
        customer.Email = $"deleted-{customer.CustomerGuid}@anonymized.invalid";
        customer.Username = null;
        customer.AdminComment = null;
        customer.LastIpAddress = null;
        customer.Active = false;
        customer.Deleted = true;
        _customerRepository.Update(customer);

        // Log the deletion
        await InsertLogAsync(customer, GdprRequestType.DeleteCustomer, "Customer permanently deleted and anonymized");
    }

    private void DeleteForumContent(int customerId)
    {
        var posts = _forumPostRepository.Table.Where(fp => fp.CustomerId == customerId).ToList();
        _forumPostRepository.Delete(posts);

        var topics = _forumTopicRepository.Table.Where(ft => ft.CustomerId == customerId).ToList();
        _forumTopicRepository.Delete(topics);
    }

    private void DeleteBlogComments(int customerId)
    {
        var comments = _blogCommentRepository.Table.Where(bc => bc.CustomerId == customerId).ToList();
        _blogCommentRepository.Delete(comments);
    }

    private void DeleteNewsComments(int customerId)
    {
        var comments = _newsCommentRepository.Table.Where(nc => nc.CustomerId == customerId).ToList();
        _newsCommentRepository.Delete(comments);
    }

    private void DeleteProductReviews(int customerId)
    {
        var reviews = _productReviewRepository.Table.Where(pr => pr.CustomerId == customerId).ToList();
        foreach (var review in reviews)
        {
            var helpfulness = _productReviewHelpfulnessRepository.Table
                .Where(prh => prh.ProductReviewId == review.Id).ToList();
            _productReviewHelpfulnessRepository.Delete(helpfulness);
        }
        _productReviewRepository.Delete(reviews);
    }

    private void DeleteActivityLogs(int customerId)
    {
        var logs = _activityLogRepository.Table.Where(al => al.CustomerId == customerId).ToList();
        _activityLogRepository.Delete(logs);
    }

    private void DeleteSystemLogs(int customerId)
    {
        var logs = _logRepository.Table.Where(l => l.CustomerId == customerId).ToList();
        _logRepository.Delete(logs);
    }

    private void DeleteShoppingCartItems(int customerId)
    {
        var items = _shoppingCartItemRepository.Table.Where(sci => sci.CustomerId == customerId).ToList();
        _shoppingCartItemRepository.Delete(items);
    }

    private void DeleteBackInStockSubscriptions(int customerId)
    {
        var subs = _backInStockSubscriptionRepository.Table.Where(biss => biss.CustomerId == customerId).ToList();
        _backInStockSubscriptionRepository.Delete(subs);
    }

    private void DeletePrivateMessages(int customerId)
    {
        var messages = _privateMessageRepository.Table
            .Where(pm => pm.FromCustomerId == customerId || pm.ToCustomerId == customerId).ToList();
        _privateMessageRepository.Delete(messages);
    }

    private void DeleteForumSubscriptions(int customerId)
    {
        var subs = _forumSubscriptionRepository.Table.Where(fs => fs.CustomerId == customerId).ToList();
        _forumSubscriptionRepository.Delete(subs);
    }

    private void DeleteGenericAttributes(int customerId)
    {
        var attrs = _genericAttributeRepository.Table
            .Where(ga => ga.EntityId == customerId && ga.KeyGroup == "Customer").ToList();
        _genericAttributeRepository.Delete(attrs);
    }

    private void DeleteCustomerPasswords(int customerId)
    {
        var passwords = _customerPasswordRepository.Table.Where(cp => cp.CustomerId == customerId).ToList();
        _customerPasswordRepository.Delete(passwords);
    }

    private void DeleteCustomerRoleMappings(int customerId)
    {
        var mappings = _customerRoleMappingRepository.Table.Where(crm => crm.CustomerId == customerId).ToList();
        _customerRoleMappingRepository.Delete(mappings);
    }

    private void AnonymizeOrderAddresses(int customerId)
    {
        var orders = _orderRepository.Table.Where(o => o.CustomerId == customerId).ToList();
        var addressIds = orders
            .SelectMany(o => new[] { o.BillingAddressId, o.ShippingAddressId ?? 0, o.PickupAddressId ?? 0 })
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (addressIds.Count == 0)
            return;

        var addresses = _addressRepository.Table.Where(a => addressIds.Contains(a.Id)).ToList();
        foreach (var address in addresses)
        {
            address.FirstName = "Deleted";
            address.LastName = "Customer";
            address.Email = null;
            address.Company = null;
            address.Address1 = null;
            address.Address2 = null;
            address.PhoneNumber = null;
            address.FaxNumber = null;
            address.CustomAttributes = null;
        }
        _addressRepository.Update(addresses);
    }

    #endregion
}
