using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Binds a single comma-separated request value onto a collection-typed member
    /// </summary>
    /// <remarks>
    /// Task 6.2: <c>System.Web.Mvc.DefaultModelBinder</c> -&gt;
    /// <see cref="IModelBinder"/>.
    /// <list type="bullet">
    /// <item>The four call sites in Nop.Admin - <c>CustomerController</c> line ~799 and
    /// <c>OrderController</c> lines ~1028-1030 - use <c>[ModelBinder(typeof(CommaSeparatedModelBinder))]</c>
    /// on an action parameter. That syntax is UNCHANGED: ASP.NET Core's
    /// <c>ModelBinderAttribute</c> has a <c>ModelBinderAttribute(Type)</c> constructor, and a
    /// parameter-level binder type is exactly the ASP.NET Core
    /// <c>BinderTypeModelBinder</c> semantic this class now implements. Those call sites need no
    /// edit in tasks 8.3/8.x beyond the <c>using</c>.</item>
    /// <item>The <c>GetPropertyValue</c> override was DROPPED - ASP.NET Core has no such seam.
    /// It existed so the binder could also be applied to a whole model and reach its collection
    /// properties; no in-tree call site did that (all four are parameter-level), so nothing
    /// observable is lost. A property can still be annotated directly with
    /// <c>[ModelBinder(typeof(CommaSeparatedModelBinder))]</c>.</item>
    /// <item>The <c>?? base.BindModel(...)</c> fallback was DROPPED for the same reason (no
    /// base). When the CSV path does not apply the binder now leaves
    /// <see cref="ModelBindingContext.Result"/> unset, which MVC treats as "not bound" and the
    /// member keeps its default - the closest available equivalent, and the same outcome for a
    /// missing value.</item>
    /// <item><c>ValueProviderResult.AttemptedValue</c> -&gt;
    /// <see cref="ValueProviderResult.FirstValue"/>; a missing value is
    /// <see cref="ValueProviderResult.None"/> rather than null.</item>
    /// </list>
    /// The splitting/conversion logic itself (split on ',', skip whitespace-only entries,
    /// <see cref="Convert.ChangeType(object, Type)"/> per element, materialise as an array when the
    /// member is an array) is carried over verbatim.
    /// </remarks>
    public class CommaSeparatedModelBinder : IModelBinder
    {
        private static readonly MethodInfo ToArrayMethod = typeof(Enumerable).GetMethod("ToArray");

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException("bindingContext");

            var model = BindCsv(bindingContext.ModelType, bindingContext.ModelName, bindingContext);
            if (model != null)
                bindingContext.Result = ModelBindingResult.Success(model);

            return Task.CompletedTask;
        }

        private object BindCsv(Type type, string name, ModelBindingContext bindingContext)
        {
            if (type.GetInterface(typeof(IEnumerable).Name) != null)
            {
                var actualValue = bindingContext.ValueProvider.GetValue(name);

                if (actualValue != ValueProviderResult.None && actualValue.FirstValue != null)
                {
                    var valueType = type.GetElementType() ?? type.GetGenericArguments().FirstOrDefault();

                    if (valueType != null && valueType.GetInterface(typeof(IConvertible).Name) != null)
                    {
                        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(valueType));

                        foreach (var splitValue in actualValue.FirstValue.Split(new[] { ',' }))
                        {
                            if (!String.IsNullOrWhiteSpace(splitValue))
                                list.Add(Convert.ChangeType(splitValue, valueType));
                        }

                        if (type.IsArray)
                            return ToArrayMethod.MakeGenericMethod(valueType).Invoke(this, new[] { list });
                        
                        return list;
                    }
                }
            }

            return null;
        }
    }
}
