using System;
using Microsoft.EntityFrameworkCore;
using Nop.Core;

namespace Nop.Data
{
    public static class Extensions
    {
        /// <summary>
        /// Get unproxied entity type
        /// </summary>
        /// <remarks>
        /// In EF Core, proxy types are handled differently. This method returns the 
        /// actual entity type (unwrapping Castle/lazy-loading proxies if present).
        /// </remarks>
        /// <param name="entity">Entity instance</param>
        /// <returns>Unproxied type</returns>
        public static Type GetUnproxiedEntityType(this BaseEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var type = entity.GetType();

            // EF Core lazy-loading proxies generate types like "Castle.Proxies.CustomerProxy"
            // The base type of the proxy IS the entity type
            if (type.Namespace == "Castle.Proxies" || type.Assembly.IsDynamic)
                return type.BaseType;

            return type;
        }
    }
}
