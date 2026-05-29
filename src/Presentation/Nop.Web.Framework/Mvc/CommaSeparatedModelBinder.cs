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
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;
            if (string.IsNullOrEmpty(value))
                return Task.CompletedTask;

            var modelType = bindingContext.ModelType;
            if (modelType.GetInterface(typeof(IEnumerable).Name) == null)
                return Task.CompletedTask;

            var valueType = modelType.GetElementType() ?? modelType.GetGenericArguments().FirstOrDefault();
            if (valueType == null || valueType.GetInterface(typeof(IConvertible).Name) == null)
                return Task.CompletedTask;

            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(valueType));

            foreach (var splitValue in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(splitValue))
                    list.Add(Convert.ChangeType(splitValue.Trim(), valueType));
            }

            object result;
            if (modelType.IsArray)
            {
                var toArrayMethod = typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(valueType);
                result = toArrayMethod.Invoke(null, new object[] { list });
            }
            else
            {
                result = list;
            }

            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }
    }

    public class CommaSeparatedModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            // This provider can be registered for specific types if needed
            return null;
        }
    }
}
