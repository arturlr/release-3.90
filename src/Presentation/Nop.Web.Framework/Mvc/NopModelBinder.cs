using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Nop.Web.Framework.Mvc
{
    public class NopModelBinder : IModelBinder
    {
        private readonly IModelBinder _fallbackBinder;

        public NopModelBinder(IModelBinder fallbackBinder)
        {
            _fallbackBinder = fallbackBinder;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            await _fallbackBinder.BindModelAsync(bindingContext);

            if (bindingContext.Result.IsModelSet)
            {
                var model = bindingContext.Result.Model;

                // Trim string properties unless marked with [NoTrim]
                if (model != null)
                {
                    var properties = model.GetType().GetProperties()
                        .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

                    foreach (var prop in properties)
                    {
                        if (prop.GetCustomAttributes(typeof(NoTrimAttribute), true).Any())
                            continue;

                        var val = prop.GetValue(model) as string;
                        if (!string.IsNullOrEmpty(val))
                            prop.SetValue(model, val.Trim());
                    }
                }

                // Call BindModel on BaseNopModel
                if (model is BaseNopModel nopModel)
                {
                    nopModel.BindModel(bindingContext.ActionContext, bindingContext);
                }
            }
        }
    }

    public class NopModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (typeof(BaseNopModel).IsAssignableFrom(context.Metadata.ModelType))
            {
                return new NopModelBinder(new SimpleTypeModelBinder(context.Metadata.ModelType, null));
            }

            return null;
        }
    }
}
