using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nop.Web.Framework.Kendoui
{
    public static class ModelStateExtensions
    {
        private static Dictionary<string, object> SerializeErrors(this ModelStateDictionary modelState)
        {
            return modelState.Where(entry => entry.Value.Errors.Any())
                .ToDictionary(
                    entry => entry.Key,
                    entry => (object)SerializeModelState(entry.Value));
        }

        private static Dictionary<string, object> SerializeModelState(ModelStateEntry modelState)
        {
            var errors = new Dictionary<string, object>();
            errors["errors"] = modelState.Errors.Select(x => x.ErrorMessage).ToArray();
            return errors;
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
