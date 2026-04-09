using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Topics;
using Nop.Services.Events;
using Nop.Services.Stores;

namespace Nop.Services.Topics;

public class TopicService : ITopicService
{
    private const string TopicsByIdKey = "Nop.topics.id-{0}";
    private const string TopicsAllKey = "Nop.topics.all-{0}-{1}-{2}";
    private const string TopicsPrefix = "Nop.topics.";

    private readonly IRepository<Topic> _topicRepository;
    private readonly IRepository<AclRecord> _aclRepository;
    private readonly IRepository<StoreMapping> _storeMappingRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IWorkContext _workContext;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly IStoreMappingService _storeMappingService;
    private readonly CatalogSettings _catalogSettings;

    public TopicService(
        IRepository<Topic> topicRepository,
        IRepository<AclRecord> aclRepository,
        IRepository<StoreMapping> storeMappingRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IWorkContext workContext,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher,
        IStoreMappingService storeMappingService,
        CatalogSettings catalogSettings)
    {
        _topicRepository = topicRepository;
        _aclRepository = aclRepository;
        _storeMappingRepository = storeMappingRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _workContext = workContext;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _storeMappingService = storeMappingService;
        _catalogSettings = catalogSettings;
    }

    public virtual async Task<Topic?> GetTopicByIdAsync(int topicId)
    {
        if (topicId == 0)
            return null;

        var key = new CacheKey(string.Format(TopicsByIdKey, topicId), TopicsPrefix);
        return await _cacheManager.GetAsync(key, () => Task.FromResult(_topicRepository.GetById(topicId)));
    }

    public virtual async Task<Topic?> GetTopicBySystemNameAsync(string systemName, int storeId = 0)
    {
        if (string.IsNullOrEmpty(systemName))
            return null;

        var topics = _topicRepository.TableNoTracking
            .Where(t => t.SystemName == systemName)
            .OrderBy(t => t.Id)
            .ToList();

        if (storeId > 0)
        {
            var filtered = new List<Topic>();
            foreach (var t in topics)
            {
                if (await _storeMappingService.AuthorizeAsync(t, storeId))
                    filtered.Add(t);
            }
            return filtered.FirstOrDefault();
        }

        return topics.FirstOrDefault();
    }

    public virtual async Task<IList<Topic>> GetAllTopicsAsync(int storeId, bool ignoreAcl = false, bool showHidden = false)
    {
        var key = new CacheKey(string.Format(TopicsAllKey, storeId, ignoreAcl, showHidden), TopicsPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _topicRepository.TableNoTracking.AsQueryable();

            if (!showHidden)
                query = query.Where(t => t.Published);

            var needsFiltering = (!ignoreAcl && !_catalogSettings.IgnoreAcl) ||
                                 (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations);

            if (needsFiltering)
            {
                if (!ignoreAcl && !_catalogSettings.IgnoreAcl)
                {
                    var customerRoleIds = _customerRoleMappingRepository.TableNoTracking
                        .Where(m => m.CustomerId == _workContext.CurrentCustomer.Id)
                        .Select(m => m.CustomerRoleId)
                        .ToList();

                    query = from t in query
                            join acl in _aclRepository.TableNoTracking
                                on new { c1 = t.Id, c2 = "Topic" } equals new { c1 = acl.EntityId, c2 = acl.EntityName } into t_acl
                            from acl in t_acl.DefaultIfEmpty()
                            where !t.SubjectToAcl || customerRoleIds.Contains(acl.CustomerRoleId)
                            select t;
                }

                if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
                {
                    query = from t in query
                            join sm in _storeMappingRepository.TableNoTracking
                                on new { c1 = t.Id, c2 = "Topic" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into t_sm
                            from sm in t_sm.DefaultIfEmpty()
                            where !t.LimitedToStores || storeId == sm.StoreId
                            select t;
                }

                query = from t in query
                        group t by t.Id into tGroup
                        orderby tGroup.Key
                        select tGroup.First();
            }

            query = query.OrderBy(t => t.DisplayOrder).ThenBy(t => t.SystemName);

            return Task.FromResult<IList<Topic>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task InsertTopicAsync(Topic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);
        _topicRepository.Insert(topic);
        await _cacheManager.RemoveByPrefixAsync(TopicsPrefix);
        await _eventPublisher.EntityInsertedAsync(topic);
    }

    public virtual async Task UpdateTopicAsync(Topic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);
        _topicRepository.Update(topic);
        await _cacheManager.RemoveByPrefixAsync(TopicsPrefix);
        await _eventPublisher.EntityUpdatedAsync(topic);
    }

    public virtual async Task DeleteTopicAsync(Topic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);
        _topicRepository.Delete(topic);
        await _cacheManager.RemoveByPrefixAsync(TopicsPrefix);
        await _eventPublisher.EntityDeletedAsync(topic);
    }
}
