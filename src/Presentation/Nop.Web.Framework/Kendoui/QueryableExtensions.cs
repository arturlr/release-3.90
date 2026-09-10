using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Nop.Web.Framework.Kendoui
{
    /// <summary>
    /// Task 6.2: <c>System.Linq.Dynamic</c> (net40-only, unmaintained) -&gt;
    /// <c>System.Linq.Dynamic.Core</c>.
    /// NOTE FOR THE RECORD - task 6.1 reported that the old <c>using System.Linq.Dynamic;</c>
    /// "still resolves as-is with the .Core package". It does, but only because
    /// <c>System.Linq.Dynamic</c> exists as a PARENT namespace of
    /// <c>System.Linq.Dynamic.Core</c>, so the directive binds to an empty namespace and is
    /// legal. The string-predicate <c>Where</c>/<c>OrderBy</c> extension methods live in
    /// <c>System.Linq.Dynamic.Core.DynamicQueryableExtensions</c> and are NOT visible through it,
    /// which only shows up once method bodies bind. The <c>using</c> therefore did have to
    /// change. Call syntax and the expression language are unchanged.
    /// </summary>
    public static class QueryableExtensions
    {
        public static IQueryable<T> Filter<T>(this IQueryable<T> queryable, Filter filter)
        {
            if (filter != null && filter.Logic != null)
            {
                // Collect a flat list of all filters
                var filters = filter.All();

                // Get all filter values as array (needed by the Where method of Dynamic Linq)
                var values = filters.Select(f => f.Value).ToArray();

                // Create a predicate expression e.g. Field1 = @0 And Field2 > @1
                string predicate = filter.ToExpression(filters);

                // Use the Where method of Dynamic Linq to filter the data
                queryable = queryable.Where(predicate, values);
            }

            return queryable;
        }

        public static IQueryable<T> Sort<T>(this IQueryable<T> queryable, IEnumerable<Sort> sort)
        {
            if (sort != null && sort.Any())
            {
                // Create ordering expression e.g. Field1 asc, Field2 desc
                var ordering = string.Join(",", sort.Select(s => s.ToExpression()));

                // Use the OrderBy method of Dynamic Linq to sort the data
                return queryable.OrderBy(ordering);
            }

            return queryable;
        }
    }
}
