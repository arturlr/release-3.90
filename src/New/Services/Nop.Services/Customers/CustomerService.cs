using System.Globalization;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Polls;
using Nop.Services.Common;
using Nop.Services.Events;

namespace Nop.Services.Customers;

public class CustomerService : ICustomerService
{
    private const string RolesAllKey = "Nop.customerrole.all-{0}";
    private const string RolesBySystemNameKey = "Nop.customerrole.systemname-{0}";
    private const string RolesPrefix = "Nop.customerrole.";

    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<CustomerPassword> _customerPasswordRepository;
    private readonly IRepository<CustomerRole> _customerRoleRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IRepository<GenericAttribute> _gaRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IRepository<ForumPost> _forumPostRepository;
    private readonly IRepository<ForumTopic> _forumTopicRepository;
    private readonly IRepository<BlogComment> _blogCommentRepository;
    private readonly IRepository<NewsComment> _newsCommentRepository;
    private readonly IRepository<PollVotingRecord> _pollVotingRecordRepository;
    private readonly IRepository<ShoppingCartItem> _shoppingCartItemRepository;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly CustomerSettings _customerSettings;

    public CustomerService(
        IRepository<Customer> customerRepository,
        IRepository<CustomerPassword> customerPasswordRepository,
        IRepository<CustomerRole> customerRoleRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IRepository<GenericAttribute> gaRepository,
        IRepository<Order> orderRepository,
        IRepository<ForumPost> forumPostRepository,
        IRepository<ForumTopic> forumTopicRepository,
        IRepository<BlogComment> blogCommentRepository,
        IRepository<NewsComment> newsCommentRepository,
        IRepository<PollVotingRecord> pollVotingRecordRepository,
        IRepository<ShoppingCartItem> shoppingCartItemRepository,
        IGenericAttributeService genericAttributeService,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher,
        CustomerSettings customerSettings)
    {
        _customerRepository = customerRepository;
        _customerPasswordRepository = customerPasswordRepository;
        _customerRoleRepository = customerRoleRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _gaRepository = gaRepository;
        _orderRepository = orderRepository;
        _forumPostRepository = forumPostRepository;
        _forumTopicRepository = forumTopicRepository;
        _blogCommentRepository = blogCommentRepository;
        _newsCommentRepository = newsCommentRepository;
        _pollVotingRecordRepository = pollVotingRecordRepository;
        _shoppingCartItemRepository = shoppingCartItemRepository;
        _genericAttributeService = genericAttributeService;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _customerSettings = customerSettings;
    }

    #region Customers

    public virtual Task<IPagedList<Customer>> GetAllCustomersAsync(
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int affiliateId = 0, int vendorId = 0, int[]? customerRoleIds = null,
        string? email = null, string? username = null,
        string? firstName = null, string? lastName = null,
        int dayOfBirth = 0, int monthOfBirth = 0,
        string? company = null, string? phone = null, string? zipPostalCode = null,
        string? ipAddress = null,
        bool loadOnlyWithShoppingCart = false, ShoppingCartType? sct = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _customerRepository.Table.Where(c => !c.Deleted);

        if (createdFromUtc.HasValue)
            query = query.Where(c => createdFromUtc.Value <= c.CreatedOnUtc);
        if (createdToUtc.HasValue)
            query = query.Where(c => createdToUtc.Value >= c.CreatedOnUtc);
        if (affiliateId > 0)
            query = query.Where(c => c.AffiliateId == affiliateId);
        if (vendorId > 0)
            query = query.Where(c => c.VendorId == vendorId);

        if (customerRoleIds is { Length: > 0 })
        {
            query = from c in query
                    join crm in _customerRoleMappingRepository.TableNoTracking
                        on c.Id equals crm.CustomerId
                    where customerRoleIds.Contains(crm.CustomerRoleId)
                    select c;
            query = query.Distinct();
        }

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(c => c.Email != null && c.Email.Contains(email));
        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(c => c.Username != null && c.Username.Contains(username));

        if (!string.IsNullOrWhiteSpace(firstName))
            query = FilterByGenericAttribute(query, SystemCustomerAttributeNames.FirstName, firstName);
        if (!string.IsNullOrWhiteSpace(lastName))
            query = FilterByGenericAttribute(query, SystemCustomerAttributeNames.LastName, lastName);

        if (dayOfBirth > 0 && monthOfBirth > 0)
        {
            var dateOfBirthStr = monthOfBirth.ToString("00", CultureInfo.InvariantCulture) + "-" +
                                 dayOfBirth.ToString("00", CultureInfo.InvariantCulture);
            query = FilterByGenericAttributeSubstring(query, SystemCustomerAttributeNames.DateOfBirth, dateOfBirthStr, 5, 5);
        }
        else if (dayOfBirth > 0)
        {
            var dateOfBirthStr = dayOfBirth.ToString("00", CultureInfo.InvariantCulture);
            query = FilterByGenericAttributeSubstring(query, SystemCustomerAttributeNames.DateOfBirth, dateOfBirthStr, 8, 2);
        }
        else if (monthOfBirth > 0)
        {
            var dateOfBirthStr = "-" + monthOfBirth.ToString("00", CultureInfo.InvariantCulture) + "-";
            query = FilterByGenericAttribute(query, SystemCustomerAttributeNames.DateOfBirth, dateOfBirthStr);
        }

        if (!string.IsNullOrWhiteSpace(company))
            query = FilterByGenericAttribute(query, SystemCustomerAttributeNames.Company, company);
        if (!string.IsNullOrWhiteSpace(phone))
            query = FilterByGenericAttribute(query, SystemCustomerAttributeNames.Phone, phone);
        if (!string.IsNullOrWhiteSpace(zipPostalCode))
            query = FilterByGenericAttribute(query, SystemCustomerAttributeNames.ZipPostalCode, zipPostalCode);

        if (!string.IsNullOrWhiteSpace(ipAddress) && CommonHelper.IsValidIpAddress(ipAddress))
            query = query.Where(c => c.LastIpAddress == ipAddress);

        if (loadOnlyWithShoppingCart)
        {
            var sctId = sct.HasValue ? (int)sct.Value : (int?)null;
            query = from c in query
                    join sci in _shoppingCartItemRepository.TableNoTracking on c.Id equals sci.CustomerId
                    where !sctId.HasValue || sci.ShoppingCartTypeId == sctId.Value
                    select c;
            query = query.Distinct();
        }

        query = query.OrderByDescending(c => c.CreatedOnUtc);

        return Task.FromResult<IPagedList<Customer>>(new PagedList<Customer>(query, pageIndex, pageSize));
    }

    public virtual Task<IPagedList<Customer>> GetOnlineCustomersAsync(DateTime lastActivityFromUtc,
        int[]? customerRoleIds = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _customerRepository.Table
            .Where(c => !c.Deleted && lastActivityFromUtc <= c.LastActivityDateUtc);

        if (customerRoleIds is { Length: > 0 })
        {
            query = from c in query
                    join crm in _customerRoleMappingRepository.TableNoTracking
                        on c.Id equals crm.CustomerId
                    where customerRoleIds.Contains(crm.CustomerRoleId)
                    select c;
            query = query.Distinct();
        }

        query = query.OrderByDescending(c => c.LastActivityDateUtc);
        return Task.FromResult<IPagedList<Customer>>(new PagedList<Customer>(query, pageIndex, pageSize));
    }

    public virtual async Task DeleteCustomerAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (customer.IsSystemAccount)
            throw new NopException($"System customer account ({customer.SystemName}) could not be deleted");

        customer.Deleted = true;

        if (_customerSettings.SuffixDeletedCustomers)
        {
            if (!string.IsNullOrEmpty(customer.Email))
                customer.Email += "-DELETED";
            if (!string.IsNullOrEmpty(customer.Username))
                customer.Username += "-DELETED";
        }

        _customerRepository.Update(customer);
        await _eventPublisher.EntityDeletedAsync(customer);
    }

    public virtual Task<Customer?> GetCustomerByIdAsync(int customerId)
    {
        if (customerId == 0)
            return Task.FromResult<Customer?>(null);
        return Task.FromResult(_customerRepository.GetById(customerId));
    }

    public virtual Task<IList<Customer>> GetCustomersByIdsAsync(int[] customerIds)
    {
        if (customerIds == null || customerIds.Length == 0)
            return Task.FromResult<IList<Customer>>([]);

        var query = _customerRepository.Table
            .Where(c => customerIds.Contains(c.Id) && !c.Deleted);
        var customers = query.ToList();

        // sort by passed identifiers
        var sorted = new List<Customer>();
        foreach (var id in customerIds)
        {
            var customer = customers.Find(x => x.Id == id);
            if (customer != null)
                sorted.Add(customer);
        }
        return Task.FromResult<IList<Customer>>(sorted);
    }

    public virtual Task<Customer?> GetCustomerByGuidAsync(Guid customerGuid)
    {
        if (customerGuid == Guid.Empty)
            return Task.FromResult<Customer?>(null);

        return Task.FromResult(
            _customerRepository.Table
                .Where(c => c.CustomerGuid == customerGuid)
                .OrderBy(c => c.Id)
                .FirstOrDefault());
    }

    public virtual Task<Customer?> GetCustomerByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult<Customer?>(null);

        return Task.FromResult(
            _customerRepository.Table
                .Where(c => c.Email == email)
                .OrderBy(c => c.Id)
                .FirstOrDefault());
    }

    public virtual Task<Customer?> GetCustomerBySystemNameAsync(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return Task.FromResult<Customer?>(null);

        return Task.FromResult(
            _customerRepository.Table
                .Where(c => c.SystemName == systemName)
                .OrderBy(c => c.Id)
                .FirstOrDefault());
    }

    public virtual Task<Customer?> GetCustomerByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Task.FromResult<Customer?>(null);

        return Task.FromResult(
            _customerRepository.Table
                .Where(c => c.Username == username)
                .OrderBy(c => c.Id)
                .FirstOrDefault());
    }

    public virtual async Task<Customer> InsertGuestCustomerAsync()
    {
        var customer = new Customer
        {
            CustomerGuid = Guid.NewGuid(),
            Active = true,
            CreatedOnUtc = DateTime.UtcNow,
            LastActivityDateUtc = DateTime.UtcNow,
        };

        var guestRole = await GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests)
            ?? throw new NopException("'Guests' role could not be loaded");

        _customerRepository.Insert(customer);

        _customerRoleMappingRepository.Insert(new CustomerCustomerRoleMapping
        {
            CustomerId = customer.Id,
            CustomerRoleId = guestRole.Id
        });

        return customer;
    }

    public virtual async Task InsertCustomerAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        _customerRepository.Insert(customer);
        await _eventPublisher.EntityInsertedAsync(customer);
    }

    public virtual async Task UpdateCustomerAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        _customerRepository.Update(customer);
        await _eventPublisher.EntityUpdatedAsync(customer);
    }

    public virtual async Task ResetCheckoutDataAsync(Customer customer, int storeId,
        bool clearCouponCodes = false, bool clearCheckoutAttributes = false,
        bool clearRewardPoints = true, bool clearShippingMethod = true,
        bool clearPaymentMethod = true)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (clearCouponCodes)
        {
            await _genericAttributeService.SaveAttributeAsync<string?>(customer, SystemCustomerAttributeNames.DiscountCouponCode, null);
            await _genericAttributeService.SaveAttributeAsync<string?>(customer, SystemCustomerAttributeNames.GiftCardCouponCodes, null);
        }

        if (clearCheckoutAttributes)
            await _genericAttributeService.SaveAttributeAsync<string?>(customer, SystemCustomerAttributeNames.CheckoutAttributes, null, storeId);

        if (clearRewardPoints)
            await _genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.UseRewardPointsDuringCheckout, false, storeId);

        if (clearShippingMethod)
        {
            await _genericAttributeService.SaveAttributeAsync<string?>(customer, SystemCustomerAttributeNames.SelectedShippingOption, null, storeId);
            await _genericAttributeService.SaveAttributeAsync<string?>(customer, SystemCustomerAttributeNames.OfferedShippingOptions, null, storeId);
            await _genericAttributeService.SaveAttributeAsync<string?>(customer, SystemCustomerAttributeNames.SelectedPickupPoint, null, storeId);
        }

        if (clearPaymentMethod)
            await _genericAttributeService.SaveAttributeAsync<string?>(customer, SystemCustomerAttributeNames.SelectedPaymentMethod, null, storeId);

        await UpdateCustomerAsync(customer);
    }

    public virtual Task<int> DeleteGuestCustomersAsync(DateTime? createdFromUtc, DateTime? createdToUtc, bool onlyWithoutShoppingCart)
    {
        var guestRole = _customerRoleRepository.TableNoTracking
            .FirstOrDefault(cr => cr.SystemName == SystemCustomerRoleNames.Guests);
        if (guestRole == null)
            throw new NopException("'Guests' role could not be loaded");

        var query = from c in _customerRepository.Table
                    join crm in _customerRoleMappingRepository.TableNoTracking
                        on c.Id equals crm.CustomerId
                    where crm.CustomerRoleId == guestRole.Id
                    select c;

        if (createdFromUtc.HasValue)
            query = query.Where(c => createdFromUtc.Value <= c.CreatedOnUtc);
        if (createdToUtc.HasValue)
            query = query.Where(c => createdToUtc.Value >= c.CreatedOnUtc);

        if (onlyWithoutShoppingCart)
            query = query.Where(c => !_shoppingCartItemRepository.TableNoTracking.Any(sci => sci.CustomerId == c.Id));

        // exclude customers with orders
        query = query.Where(c => !_orderRepository.TableNoTracking.Any(o => o.CustomerId == c.Id));
        // exclude customers with blog comments
        query = query.Where(c => !_blogCommentRepository.TableNoTracking.Any(bc => bc.CustomerId == c.Id));
        // exclude customers with news comments
        query = query.Where(c => !_newsCommentRepository.TableNoTracking.Any(nc => nc.CustomerId == c.Id));
        // exclude customers with poll votes
        query = query.Where(c => !_pollVotingRecordRepository.TableNoTracking.Any(pvr => pvr.CustomerId == c.Id));
        // exclude customers with forum posts
        query = query.Where(c => !_forumPostRepository.TableNoTracking.Any(fp => fp.CustomerId == c.Id));
        // exclude customers with forum topics
        query = query.Where(c => !_forumTopicRepository.TableNoTracking.Any(ft => ft.CustomerId == c.Id));
        // exclude system accounts
        query = query.Where(c => !c.IsSystemAccount);

        query = query.Distinct().OrderBy(c => c.Id);

        var customers = query.ToList();
        var totalRecordsDeleted = 0;

        foreach (var c in customers)
        {
            try
            {
                var attributes = _gaRepository.Table
                    .Where(ga => ga.EntityId == c.Id && ga.KeyGroup == "Customer")
                    .ToList();
                if (attributes.Count > 0)
                    _gaRepository.Delete(attributes);

                // delete role mappings
                var mappings = _customerRoleMappingRepository.Table
                    .Where(crm => crm.CustomerId == c.Id)
                    .ToList();
                if (mappings.Count > 0)
                    _customerRoleMappingRepository.Delete(mappings);

                _customerRepository.Delete(c);
                totalRecordsDeleted++;
            }
            catch
            {
                // continue on individual failures
            }
        }

        return Task.FromResult(totalRecordsDeleted);
    }

    #endregion

    #region Customer roles

    public virtual async Task DeleteCustomerRoleAsync(CustomerRole customerRole)
    {
        ArgumentNullException.ThrowIfNull(customerRole);

        if (customerRole.IsSystemRole)
            throw new NopException("System role could not be deleted");

        _customerRoleRepository.Delete(customerRole);
        await _cacheManager.RemoveByPrefixAsync(RolesPrefix);
        await _eventPublisher.EntityDeletedAsync(customerRole);
    }

    public virtual Task<CustomerRole?> GetCustomerRoleByIdAsync(int customerRoleId)
    {
        if (customerRoleId == 0)
            return Task.FromResult<CustomerRole?>(null);
        return Task.FromResult(_customerRoleRepository.GetById(customerRoleId));
    }

    public virtual async Task<CustomerRole?> GetCustomerRoleBySystemNameAsync(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return null;

        var key = new CacheKey(string.Format(RolesBySystemNameKey, systemName), RolesPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_customerRoleRepository.TableNoTracking
                .Where(cr => cr.SystemName == systemName)
                .OrderBy(cr => cr.Id)
                .FirstOrDefault()));
    }

    public virtual async Task<IList<CustomerRole>> GetAllCustomerRolesAsync(bool showHidden = false)
    {
        var key = new CacheKey(string.Format(RolesAllKey, showHidden), RolesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _customerRoleRepository.TableNoTracking
                .Where(cr => showHidden || cr.Active)
                .OrderBy(cr => cr.Name);
            return Task.FromResult<IList<CustomerRole>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task InsertCustomerRoleAsync(CustomerRole customerRole)
    {
        ArgumentNullException.ThrowIfNull(customerRole);
        _customerRoleRepository.Insert(customerRole);
        await _cacheManager.RemoveByPrefixAsync(RolesPrefix);
        await _eventPublisher.EntityInsertedAsync(customerRole);
    }

    public virtual async Task UpdateCustomerRoleAsync(CustomerRole customerRole)
    {
        ArgumentNullException.ThrowIfNull(customerRole);
        _customerRoleRepository.Update(customerRole);
        await _cacheManager.RemoveByPrefixAsync(RolesPrefix);
        await _eventPublisher.EntityUpdatedAsync(customerRole);
    }

    #endregion

    #region Customer role mappings

    public virtual Task AddCustomerRoleMappingAsync(CustomerCustomerRoleMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);
        _customerRoleMappingRepository.Insert(mapping);
        return Task.CompletedTask;
    }

    public virtual Task RemoveCustomerRoleMappingAsync(Customer customer, CustomerRole role)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(role);

        var mapping = _customerRoleMappingRepository.Table
            .FirstOrDefault(crm => crm.CustomerId == customer.Id && crm.CustomerRoleId == role.Id);
        if (mapping != null)
            _customerRoleMappingRepository.Delete(mapping);

        return Task.CompletedTask;
    }

    public virtual Task<int[]> GetCustomerRoleIdsAsync(Customer customer, bool showHidden = false)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var query = from crm in _customerRoleMappingRepository.TableNoTracking
                    join cr in _customerRoleRepository.TableNoTracking on crm.CustomerRoleId equals cr.Id
                    where crm.CustomerId == customer.Id && (showHidden || cr.Active)
                    select cr.Id;

        return Task.FromResult(query.ToArray());
    }

    #endregion

    #region Customer passwords

    public virtual Task<IList<CustomerPassword>> GetCustomerPasswordsAsync(int? customerId = null,
        PasswordFormat? passwordFormat = null, int? passwordsToReturn = null)
    {
        var query = _customerPasswordRepository.Table;

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);
        if (passwordFormat.HasValue)
            query = query.Where(p => p.PasswordFormatId == (int)passwordFormat.Value);
        if (passwordsToReturn.HasValue)
            query = query.OrderByDescending(p => p.CreatedOnUtc).Take(passwordsToReturn.Value);

        return Task.FromResult<IList<CustomerPassword>>(query.ToList());
    }

    public virtual async Task<CustomerPassword?> GetCurrentPasswordAsync(int customerId)
    {
        if (customerId == 0)
            return null;
        return (await GetCustomerPasswordsAsync(customerId, passwordsToReturn: 1)).FirstOrDefault();
    }

    public virtual async Task InsertCustomerPasswordAsync(CustomerPassword customerPassword)
    {
        ArgumentNullException.ThrowIfNull(customerPassword);
        _customerPasswordRepository.Insert(customerPassword);
        await _eventPublisher.EntityInsertedAsync(customerPassword);
    }

    public virtual async Task UpdateCustomerPasswordAsync(CustomerPassword customerPassword)
    {
        ArgumentNullException.ThrowIfNull(customerPassword);
        _customerPasswordRepository.Update(customerPassword);
        await _eventPublisher.EntityUpdatedAsync(customerPassword);
    }

    #endregion

    #region Helpers

    private IQueryable<Customer> FilterByGenericAttribute(IQueryable<Customer> query, string key, string value)
    {
        return from c in query
               join ga in _gaRepository.TableNoTracking on c.Id equals ga.EntityId
               where ga.KeyGroup == "Customer" && ga.Key == key && ga.Value != null && ga.Value.Contains(value)
               select c;
    }

    private IQueryable<Customer> FilterByGenericAttributeSubstring(IQueryable<Customer> query, string key, string value, int startIndex, int length)
    {
        return from c in query
               join ga in _gaRepository.TableNoTracking on c.Id equals ga.EntityId
               where ga.KeyGroup == "Customer" && ga.Key == key && ga.Value != null && ga.Value.Substring(startIndex, length) == value
               select c;
    }

    #endregion
}
