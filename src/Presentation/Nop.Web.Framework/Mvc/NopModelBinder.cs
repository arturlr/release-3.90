using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;


namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Custom model binder that trims string values and calls BindModel on BaseNopModel.
    /// In ASP.NET Core, model binders implement IModelBinder.
    /// </summary>
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

                if (model is BaseNopModel nopModel)
                {
                    nopModel.BindModel(bindingContext);
                }

                // Trim string properties unless marked with [NoTrim]
                if (model != null)
                {
                    var properties = model.GetType().GetProperties()
                        .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

                    foreach (var prop in properties)
                    {
                        if (prop.GetCustomAttributes(typeof(NoTrimAttribute), true).Any())
                            continue;

                        var value = prop.GetValue(model) as string;
                        if (!string.IsNullOrEmpty(value))
                        {
                            prop.SetValue(model, value.Trim());
                        }
                    }
                }
            }
        }
    }
}
