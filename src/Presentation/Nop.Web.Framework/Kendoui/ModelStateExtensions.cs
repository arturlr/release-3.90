using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Nop.Web.Framework.Kendoui
{
    public static class ModelStateExtensions
    {
        private static string GetErrorMessage(ModelError error, ModelStateEntry modelState)
        {
            if (!string.IsNullOrEmpty(error.ErrorMessage))
                return error.ErrorMessage;
            if (modelState.AttemptedValue == null)
                return error.ErrorMessage;
            return string.Format("The value '{0}' is invalid", modelState.AttemptedValue);
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
                return modelState.SerializeErrors();
            return null;
        }
    }
}
