using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Events;

namespace Nop.Services.Security;

/// <summary>
/// ACL service interface
/// </summary>
public interface IAclService
{
    void DeleteAclRecord(AclRecord aclRecord);
    AclRecord? GetAclRecordById(int aclRecordId);
    IList<AclRecord> GetAclRecords<T>(T entity) where T : BaseEntity, IAclSupported;
    void InsertAclRecord(AclRecord aclRecord);
    void InsertAclRecord<T>(T entity, int customerRoleId) where T : BaseEntity, IAclSupported;
    void UpdateAclRecord(AclRecord aclRecord);
    int[] GetCustomerRoleIdsWithAccess<T>(T entity) where T : BaseEntity, IAclSupported;
    bool Authorize<T>(T entity) where T : BaseEntity, IAclSupported;
    bool Authorize<T>(T entity, Customer customer) where T : BaseEntity, IAclSupported;
}

/// <summary>
/// ACL service — entity-level access control via customer roles.
/// </summary>
public class AclService : IAclService
{
    private const string AclRecordByEntityIdNameKey = "Nop.aclrecord.entityid-name-{0}-{1}";
    private const string AclRecordPrefix = "Nop.aclrecord.";

    private readonly IRepository<AclRecord> _aclRecordRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IWorkContext _workContext;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly CatalogSettings _catalogSettings;

    public AclService(
        IRepository<AclRecord> aclRecordRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IWorkContext workContext,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher,
        CatalogSettings catalogSettings)
    {
        _aclRecordRepository = aclRecordRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _workContext = workContext;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _catalogSettings = catalogSettings;
    }

    public virtual void DeleteAclRecord(AclRecord aclRecord)
    {
        ArgumentNullException.ThrowIfNull(aclRecord);
        _aclRecordRepository.Delete(aclRecord);
        _cacheManager.RemoveByPrefixAsync(AclRecordPrefix).GetAwaiter().GetResult();
        _eventPublisher.EntityDeletedAsync(aclRecord).GetAwaiter().GetResult();
    }

    public virtual AclRecord? GetAclRecordById(int aclRecordId)
    {
        return aclRecordId == 0 ? null : _aclRecordRepository.GetById(aclRecordId);
    }

    public virtual IList<AclRecord> GetAclRecords<T>(T entity) where T : BaseEntity, IAclSupported
    {
        ArgumentNullException.ThrowIfNull(entity);
        return _aclRecordRepository.Table
            .Where(ar => ar.EntityId == entity.Id && ar.EntityName == typeof(T).Name)
            .ToList();
    }

    public virtual void InsertAclRecord(AclRecord aclRecord)
    {
        ArgumentNullException.ThrowIfNull(aclRecord);
        _aclRecordRepository.Insert(aclRecord);
        _cacheManager.RemoveByPrefixAsync(AclRecordPrefix).GetAwaiter().GetResult();
        _eventPublisher.EntityInsertedAsync(aclRecord).GetAwaiter().GetResult();
    }

    public virtual void InsertAclRecord<T>(T entity, int customerRoleId) where T : BaseEntity, IAclSupported
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentOutOfRangeException.ThrowIfZero(customerRoleId);

        InsertAclRecord(new AclRecord
        {
            EntityId = entity.Id,
            EntityName = typeof(T).Name,
            CustomerRoleId = customerRoleId
        });
    }

    public virtual void UpdateAclRecord(AclRecord aclRecord)
    {
        ArgumentNullException.ThrowIfNull(aclRecord);
        _aclRecordRepository.Update(aclRecord);
        _cacheManager.RemoveByPrefixAsync(AclRecordPrefix).GetAwaiter().GetResult();
        _eventPublisher.EntityUpdatedAsync(aclRecord).GetAwaiter().GetResult();
    }

    public virtual int[] GetCustomerRoleIdsWithAccess<T>(T entity) where T : BaseEntity, IAclSupported
    {
        ArgumentNullException.ThrowIfNull(entity);

        var key = new CacheKey(
            string.Format(AclRecordByEntityIdNameKey, entity.Id, typeof(T).Name),
            AclRecordPrefix);

        return _cacheManager.Get(key, () =>
            _aclRecordRepository.TableNoTracking
                .Where(ar => ar.EntityId == entity.Id && ar.EntityName == typeof(T).Name)
                .Select(ar => ar.CustomerRoleId)
                .ToArray()) ?? [];
    }

    public virtual bool Authorize<T>(T entity) where T : BaseEntity, IAclSupported
    {
        return Authorize(entity, _workContext.CurrentCustomer);
    }

    public virtual bool Authorize<T>(T entity, Customer customer) where T : BaseEntity, IAclSupported
    {
        if (entity == null || customer == null)
            return false;

        if (_catalogSettings.IgnoreAcl)
            return true;

        if (!entity.SubjectToAcl)
            return true;

        var allowedRoleIds = GetCustomerRoleIdsWithAccess(entity);

        // Query customer's active role IDs from join table (no nav property)
        var customerRoleIds = _customerRoleMappingRepository.TableNoTracking
            .Where(m => m.CustomerId == customer.Id)
            .Select(m => m.CustomerRoleId)
            .ToList();

        return customerRoleIds.Any(allowedRoleIds.Contains);
    }
}
