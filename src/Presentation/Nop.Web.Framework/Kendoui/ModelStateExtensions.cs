
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nop.Web.Framework.Kendoui
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core model binding.
    /// <list type="bullet">
    /// <item><c>System.Web.Mvc.ModelStateDictionary</c> -&gt;
    /// <c>Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary</c>.</item>
    /// <item><c>System.Web.Mvc.ModelState</c> -&gt;
    /// <see cref="ModelStateEntry"/> (renamed type). PUBLIC-ISH SIGNATURE CHANGE on the two
    /// private helpers' parameter type; the two public extension methods
    /// (<see cref="SerializeErrors"/>, <see cref="ToDataSourceResult"/>) keep their shape.</item>
    /// <item><c>modelState.Value.AttemptedValue</c> -&gt;
    /// <see cref="ModelStateEntry.AttemptedValue"/>. ASP.NET Core folded MVC 5's separate
    /// <c>ValueProviderResult Value</c> object into the entry itself, so the null check on
    /// <c>Value</c> becomes a null check on <c>AttemptedValue</c>.</item>
    /// <item><c>System.Web.Mvc.ModelError</c> -&gt;
    /// <c>Microsoft.AspNetCore.Mvc.ModelBinding.ModelError</c> (same members).</item>
    /// </list>
    /// The emitted JSON shape - <c>{ "&lt;key&gt;": { "errors": [ … ] } }</c>, consumed by the
    /// Kendo grid scripts - is unchanged.
    /// </summary>
    public static class ModelStateExtensions
    {
        private static string GetErrorMessage(ModelError error, ModelStateEntry modelState)
        {
            if (!string.IsNullOrEmpty(error.ErrorMessage))
            {
                return error.ErrorMessage;
            }
            if (modelState.AttemptedValue == null)
            {
                return error.ErrorMessage;
            }
            var args = new object[] { modelState.AttemptedValue };
            return string.Format("ValueNotValidForProperty=The value '{0}' is invalid", args);
        }

        public static object SerializeErrors(this ModelStateDictionary modelState)
        {
            return modelState.Where(entry => entry.Value.Errors.Any())
                .ToDictionary(entry => entry.Key, entry => SerializeModelState(entry.Value));
        }

        private static Dictionary<string, object> SerializeModelState(ModelStateEntry modelState)
        {
            var dictionary = new Dictionary<string, object>();
            dictionary["errors"] = modelState.Errors.Select(x => GetErrorMessage(x, modelState)).ToArray();
            return dictionary;
        }

        public static object ToDataSourceResult(this ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
            {
                return modelState.SerializeErrors();
            }
            return null;
        }
    }
}
