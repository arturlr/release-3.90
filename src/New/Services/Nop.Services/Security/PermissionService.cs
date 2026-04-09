using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Services.Localization;

namespace Nop.Services.Security;

/// <summary>
/// Permission service interface
/// </summary>
public interface IPermissionService
{
    void DeletePermissionRecord(PermissionRecord permission);
    PermissionRecord? GetPermissionRecordById(int permissionId);
    PermissionRecord? GetPermissionRecordBySystemName(string systemName);
    IList<PermissionRecord> GetAllPermissionRecords();
    void InsertPermissionRecord(PermissionRecord permission);
    void UpdatePermissionRecord(PermissionRecord permission);
    void InstallPermissions(IPermissionProvider permissionProvider);
    void UninstallPermissions(IPermissionProvider permissionProvider);
    bool Authorize(PermissionRecord permission);
    bool Authorize(PermissionRecord permission, Customer customer);
    bool Authorize(string permissionRecordSystemName);
    bool Authorize(string permissionRecordSystemName, Customer customer);
}

/// <summary>
/// Permission service — checks if customer roles have requested permissions.
/// Uses join tables instead of nav properties for role/permission lookups.
/// </summary>
public class PermissionService : IPermissionService
{
    private const string PermissionsAllowedKey = "Nop.permission.allowed-{0}-{1}";
    private const string PermissionsPrefix = "Nop.permission.";

    private readonly IRepository<PermissionRecord> _permissionRecordRepository;
    private readonly IRepository<PermissionRecordRoleMapping> _permissionRoleMappingRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IRepository<CustomerRole> _customerRoleRepository;
    private readonly IWorkContext _workContext;
    private readonly ILocalizationService _localizationService;
    private readonly ILanguageService _languageService;
    private readonly IStaticCacheManager _cacheManager;

    public PermissionService(
        IRepository<PermissionRecord> permissionRecordRepository,
        IRepository<PermissionRecordRoleMapping> permissionRoleMappingRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IRepository<CustomerRole> customerRoleRepository,
        IWorkContext workContext,
        ILocalizationService localizationService,
        ILanguageService languageService,
        IStaticCacheManager cacheManager)
    {
        _permissionRecordRepository = permissionRecordRepository;
        _permissionRoleMappingRepository = permissionRoleMappingRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _customerRoleRepository = customerRoleRepository;
        _workContext = workContext;
        _localizationService = localizationService;
        _languageService = languageService;
        _cacheManager = cacheManager;
    }

    /// <summary>
    /// Check if a specific customer role has a permission (cached).
    /// </summary>
    protected virtual bool Authorize(string permissionRecordSystemName, int customerRoleId)
    {
        if (string.IsNullOrEmpty(permissionRecordSystemName))
            return false;

        var key = new CacheKey(
            string.Format(PermissionsAllowedKey, customerRoleId, permissionRecordSystemName),
            PermissionsPrefix);

        return _cacheManager.Get(key, () =>
        {
            // Query via join table: does this role have a mapping to a permission with this system name?
            return (from prm in _permissionRoleMappingRepository.TableNoTracking
                    join pr in _permissionRecordRepository.TableNoTracking
                        on prm.PermissionRecordId equals pr.Id
                    where prm.CustomerRoleId == customerRoleId
                          && pr.SystemName == permissionRecordSystemName
                    select pr.Id).Any();
        });
    }

    public virtual void DeletePermissionRecord(PermissionRecord permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        _permissionRecordRepository.Delete(permission);
        _cacheManager.RemoveByPrefixAsync(PermissionsPrefix).GetAwaiter().GetResult();
    }

    public virtual PermissionRecord? GetPermissionRecordById(int permissionId)
    {
        return permissionId == 0 ? null : _permissionRecordRepository.GetById(permissionId);
    }

    public virtual PermissionRecord? GetPermissionRecordBySystemName(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            return null;

        return _permissionRecordRepository.Table
            .Where(pr => pr.SystemName == systemName)
            .OrderBy(pr => pr.Id)
            .FirstOrDefault();
    }

    public virtual IList<PermissionRecord> GetAllPermissionRecords()
    {
        return _permissionRecordRepository.Table
            .OrderBy(pr => pr.Name)
            .ToList();
    }

    public virtual void InsertPermissionRecord(PermissionRecord permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        _permissionRecordRepository.Insert(permission);
        _cacheManager.RemoveByPrefixAsync(PermissionsPrefix).GetAwaiter().GetResult();
    }

    public virtual void UpdatePermissionRecord(PermissionRecord permission)
    {
        ArgumentNullException.ThrowIfNull(permission);
        _permissionRecordRepository.Update(permission);
        _cacheManager.RemoveByPrefixAsync(PermissionsPrefix).GetAwaiter().GetResult();
    }

    public virtual void InstallPermissions(IPermissionProvider permissionProvider)
    {
        var permissions = permissionProvider.GetPermissions();
        var defaultPermissions = permissionProvider.GetDefaultPermissions().ToList();

        foreach (var permission in permissions)
        {
            var existing = GetPermissionRecordBySystemName(permission.SystemName!);
            if (existing != null)
                continue;

            // Insert the new permission record
            var newPermission = new PermissionRecord
            {
                Name = permission.Name,
                SystemName = permission.SystemName,
                Category = permission.Category
            };
            InsertPermissionRecord(newPermission);

            // Create role mappings from default permissions
            foreach (var defaultPerm in defaultPermissions)
            {
                var hasThisPermission = defaultPerm.PermissionRecords
                    .Any(p => p.SystemName == newPermission.SystemName);
                if (!hasThisPermission)
                    continue;

                var role = _customerRoleRepository.Table
                    .FirstOrDefault(cr => cr.SystemName == defaultPerm.CustomerRoleSystemName);

                if (role == null)
                {
                    role = new CustomerRole
                    {
                        Name = defaultPerm.CustomerRoleSystemName,
                        Active = true,
                        SystemName = defaultPerm.CustomerRoleSystemName
                    };
                    _customerRoleRepository.Insert(role);
                }

                // Check if mapping already exists
                var mappingExists = _permissionRoleMappingRepository.TableNoTracking
                    .Any(m => m.PermissionRecordId == newPermission.Id && m.CustomerRoleId == role.Id);

                if (!mappingExists)
                {
                    _permissionRoleMappingRepository.Insert(new PermissionRecordRoleMapping
                    {
                        PermissionRecordId = newPermission.Id,
                        CustomerRoleId = role.Id
                    });
                }
            }

            // Save localized permission name
            newPermission.SaveLocalizedPermissionName(_localizationService, _languageService);
        }
    }

    public virtual void UninstallPermissions(IPermissionProvider permissionProvider)
    {
        foreach (var permission in permissionProvider.GetPermissions())
        {
            var existing = GetPermissionRecordBySystemName(permission.SystemName!);
            if (existing == null)
                continue;

            // Delete role mappings
            var mappings = _permissionRoleMappingRepository.Table
                .Where(m => m.PermissionRecordId == existing.Id)
                .ToList();
            _permissionRoleMappingRepository.Delete(mappings);

            DeletePermissionRecord(existing);
            existing.DeleteLocalizedPermissionName(_localizationService, _languageService);
        }
    }

    public virtual bool Authorize(PermissionRecord permission)
    {
        return Authorize(permission, _workContext.CurrentCustomer);
    }

    public virtual bool Authorize(PermissionRecord permission, Customer customer)
    {
        if (permission == null || customer == null)
            return false;

        return Authorize(permission.SystemName!, customer);
    }

    public virtual bool Authorize(string permissionRecordSystemName)
    {
        return Authorize(permissionRecordSystemName, _workContext.CurrentCustomer);
    }

    public virtual bool Authorize(string permissionRecordSystemName, Customer customer)
    {
        if (string.IsNullOrEmpty(permissionRecordSystemName) || customer == null)
            return false;

        // Get customer's active role IDs via join table
        var customerRoleIds = _customerRoleMappingRepository.TableNoTracking
            .Where(m => m.CustomerId == customer.Id)
            .Join(_customerRoleRepository.TableNoTracking.Where(cr => cr.Active),
                m => m.CustomerRoleId, cr => cr.Id,
                (m, cr) => cr.Id)
            .ToList();

        foreach (var roleId in customerRoleIds)
        {
            if (Authorize(permissionRecordSystemName, roleId))
                return true;
        }

        return false;
    }
}
