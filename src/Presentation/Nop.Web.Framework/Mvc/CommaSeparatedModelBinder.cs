using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;


namespace Nop.Web.Framework.Mvc
{
    public class CommaSeparatedModelBinder : IModelBinder
    {
        private static readonly MethodInfo ToArrayMethod = typeof(Enumerable).GetMethod("ToArray");

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            var modelName = bindingContext.ModelName;
            var type = bindingContext.ModelType;

            var result = BindCsv(type, modelName, bindingContext);
            if (result != null)
            {
                bindingContext.Result = ModelBindingResult.Success(result);
            }

            return Task.CompletedTask;
        }

        private object BindCsv(Type type, string name, ModelBindingContext bindingContext)
        {
            if (type.GetInterface(typeof(IEnumerable).Name) != null)
            {
                var actualValue = bindingContext.ValueProvider.GetValue(name);

                if (actualValue != ValueProviderResult.None)
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
