using System;
using Nop.Core;

namespace Nop.Data
{
    public static class Extensions
    {
        /// <summary>
        /// Get unproxied entity type
        /// </summary>
        /// <remarks> If your Entity Framework context is proxy-enabled, 
        /// the runtime will create a proxy instance of your entities, 
        /// i.e. a dynamically generated class which inherits from your entity class 
        /// and overrides its virtual properties by inserting specific code useful for example 
        /// for tracking changes and lazy loading.
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Type GetUnproxiedEntityType(this BaseEntity entity)
        {
            var entityType = entity.GetType();

            // In EF Core, proxy types inherit from the entity type
            // The base type of a proxy is the actual entity type
            if (entityType.Namespace == "Castle.Proxies" || entityType.Assembly.IsDynamic)
            {
                return entityType.BaseType ?? entityType;
            }

            return entityType;
        }
    }
}
