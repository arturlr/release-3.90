using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Nop.Web.Framework.Kendoui
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> Filter<T>(this IQueryable<T> queryable, Filter filter)
        {
            if (filter != null && filter.Logic != null)
            {
                var filters = filter.All();
                // Apply simple property-based filtering
                foreach (var f in filters)
                {
                    if (string.IsNullOrEmpty(f.Field))
                        continue;

                    var parameter = Expression.Parameter(typeof(T), "x");
                    var property = typeof(T).GetProperty(f.Field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (property == null)
                        continue;

                    var member = Expression.Property(parameter, property);
                    var constant = Expression.Constant(f.Value != null ? Convert.ChangeType(f.Value, property.PropertyType) : null);

                    Expression comparison;
                    switch ((f.Operator ?? "eq").ToLower())
                    {
                        case "eq": comparison = Expression.Equal(member, constant); break;
                        case "neq": comparison = Expression.NotEqual(member, constant); break;
                        case "gt": comparison = Expression.GreaterThan(member, constant); break;
                        case "gte": comparison = Expression.GreaterThanOrEqual(member, constant); break;
                        case "lt": comparison = Expression.LessThan(member, constant); break;
                        case "lte": comparison = Expression.LessThanOrEqual(member, constant); break;
                        default: comparison = Expression.Equal(member, constant); break;
                    }

                    var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
                    queryable = queryable.Where(lambda);
                }
            }

            return queryable;
        }

        public static IQueryable<T> Sort<T>(this IQueryable<T> queryable, IEnumerable<Sort> sort)
        {
            if (sort != null && sort.Any())
            {
                bool first = true;
                foreach (var s in sort)
                {
                    if (string.IsNullOrEmpty(s.Field))
                        continue;

                    var parameter = Expression.Parameter(typeof(T), "x");
                    var property = typeof(T).GetProperty(s.Field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (property == null)
                        continue;

                    var member = Expression.Property(parameter, property);
                    var lambda = Expression.Lambda(member, parameter);

                    string methodName;
                    if (first)
                        methodName = string.Equals(s.Dir, "desc", StringComparison.OrdinalIgnoreCase) ? "OrderByDescending" : "OrderBy";
                    else
                        methodName = string.Equals(s.Dir, "desc", StringComparison.OrdinalIgnoreCase) ? "ThenByDescending" : "ThenBy";

                    var method = typeof(Queryable).GetMethods()
                        .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                        .MakeGenericMethod(typeof(T), property.PropertyType);

                    queryable = (IQueryable<T>)method.Invoke(null, new object[] { queryable, lambda });
                    first = false;
                }
            }

            return queryable;
        }
    }
}
