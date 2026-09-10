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
        /// <para>
        /// EF6 -> EF Core (task 3.2): EF6's <c>ObjectContext.GetObjectType(Type)</c> does not exist
        /// in EF Core. EF Core's optional proxy support (Microsoft.EntityFrameworkCore.Proxies)
        /// generates Castle DynamicProxy subclasses, so the unproxied type is recovered by
        /// detecting a dynamically emitted subclass and stepping up to its base type. When
        /// proxies are not in use (the default in EF Core) the type is returned unchanged.
        /// </para>
        /// </remarks>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Type GetUnproxiedEntityType(this BaseEntity entity)
        {
            if (entity == null)
                return null;

            var userType = entity.GetType();
            while (userType != null && IsProxyType(userType))
                userType = userType.BaseType;

            return userType ?? entity.GetType();
        }

        private static bool IsProxyType(Type type)
        {
            if (type.BaseType == null || type.BaseType == typeof(object))
                return false;

            //Castle DynamicProxy (used by Microsoft.EntityFrameworkCore.Proxies) emits into the
            //"Castle.Proxies" namespace of a dynamic assembly.
            return type.Assembly.IsDynamic
                   || string.Equals(type.Namespace, "Castle.Proxies", StringComparison.Ordinal);
        }
    }
}
