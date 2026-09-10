using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Nop.Data 
{
    /// <summary>
    /// Queryable extensions
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Include
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="queryable">Queryable</param>
        /// <param name="includeProperties">A list of properties to include</param>
        /// <returns>New queryable</returns>
        /// <remarks>
        /// EF6 -> EF Core (task 3.2): EF Core's <c>Include</c> overload taking an expression is
        /// constrained to reference types (<c>where TEntity : class</c>), so this method gains the
        /// same <c>where T : class</c> constraint. No in-tree call site is affected - all entities
        /// derive from <c>BaseEntity</c>, a class.
        /// </remarks>
        public static IQueryable<T> IncludeProperties<T>(this IQueryable<T> queryable,
            params Expression<Func<T, object>>[] includeProperties) where T : class
        {
            if (queryable == null)
                throw new ArgumentNullException("queryable");

            foreach (Expression<Func<T, object>> includeProperty in includeProperties)
                queryable = queryable.Include(includeProperty);

            return queryable;
        }

    }
}
