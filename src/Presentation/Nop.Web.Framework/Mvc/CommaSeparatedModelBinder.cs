using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nop.Web.Framework.Mvc
{
    public class CommaSeparatedModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrEmpty(value))
                return Task.CompletedTask;

            var modelType = bindingContext.ModelType;
            if (modelType.GetInterface(typeof(IEnumerable).Name) != null)
            {
                var valueType = modelType.GetElementType() ?? modelType.GetGenericArguments().FirstOrDefault();
                if (valueType != null && typeof(IConvertible).IsAssignableFrom(valueType))
                {
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(valueType));
                    foreach (var splitValue in value.Split(new[] { ',' }))
                    {
                        if (!String.IsNullOrWhiteSpace(splitValue))
                            list.Add(Convert.ChangeType(splitValue.Trim(), valueType));
                    }

                    object result;
                    if (modelType.IsArray)
                    {
                        var toArrayMethod = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(valueType);
                        result = toArrayMethod.Invoke(null, new[] { list });
                    }
                    else
                    {
                        result = list;
                    }

                    bindingContext.Result = ModelBindingResult.Success(result);
                }
            }

            return Task.CompletedTask;
        }
    }

    public class CommaSeparatedModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            // This provider is registered manually for specific parameters
            return null;
        }
    }
}
