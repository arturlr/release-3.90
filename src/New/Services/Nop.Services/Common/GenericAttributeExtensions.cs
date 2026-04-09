using Nop.Core;

namespace Nop.Services.Common;

/// <summary>
/// Extension methods for reading generic attributes from entities.
/// No service locator — IGenericAttributeService must be passed explicitly.
/// </summary>
public static class GenericAttributeExtensions
{
    public static async Task<TPropType?> GetAttributeAsync<TPropType>(this BaseEntity entity,
        string key, IGenericAttributeService genericAttributeService, int storeId = 0)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var keyGroup = entity.GetType().Name;
        var props = await genericAttributeService.GetAttributesForEntityAsync(entity.Id, keyGroup);
        var prop = props
            .Where(x => x.StoreId == storeId)
            .FirstOrDefault(ga => ga.Key!.Equals(key, StringComparison.InvariantCultureIgnoreCase));

        if (prop == null || string.IsNullOrEmpty(prop.Value))
            return default;

        return CommonHelper.To<TPropType>(prop.Value);
    }
}
