using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Attribute to validate whether a certain form name (or value) was submitted
    /// </summary>
    public class FormValueRequiredAttribute : ActionMethodSelectorAttribute
    {
        private readonly string[] _submitButtonNames;
        private readonly FormValueRequirement _requirement;
        private readonly bool _validateNameOnly;

        public FormValueRequiredAttribute(params string[] submitButtonNames):
            this(FormValueRequirement.Equal, submitButtonNames)
        {
        }
        public FormValueRequiredAttribute(FormValueRequirement requirement, params string[] submitButtonNames):
            this(requirement, true, submitButtonNames)
        {
        }
        public FormValueRequiredAttribute(FormValueRequirement requirement, bool validateNameOnly, params string[] submitButtonNames)
        {
            //at least one submit button should be found
            this._submitButtonNames = submitButtonNames;
            this._validateNameOnly = validateNameOnly;
            this._requirement = requirement;
        }

        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
            var httpContext = routeContext.HttpContext;
            
            // Form is only available for POST requests with form content type
            if (!string.Equals(httpContext.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!httpContext.Request.HasFormContentType)
                return false;

            var form = httpContext.Request.Form;

            foreach (string buttonName in _submitButtonNames)
            {
                try
                {
                    switch (this._requirement)
                    {
                        case FormValueRequirement.Equal:
                            {
                                if (_validateNameOnly)
                                {
                                    //"name" only
                                    if (form.Keys.Any(x => x.Equals(buttonName, StringComparison.InvariantCultureIgnoreCase)))
                                        return true;
                                }
                                else
                                {
                                    //validate "value"
                                    //do not iterate because "Invalid request" exception can be thrown
                                    string value = form[buttonName];
                                    if (!String.IsNullOrEmpty(value))
                                        return true;
                                }
                            }
                            break;
                        case FormValueRequirement.StartsWith:
                            {
                                if (_validateNameOnly)
                                {
                                    //"name" only
                                    if (form.Keys.Any(x => x.StartsWith(buttonName, StringComparison.InvariantCultureIgnoreCase)))
                                        return true;
                                }
                                else
                                {
                                    //validate "value"
                                    foreach (var formKey in form.Keys)
                                        if (formKey.StartsWith(buttonName, StringComparison.InvariantCultureIgnoreCase))
                                        { 
                                            var value = form[formKey].ToString();
                                            if (!String.IsNullOrEmpty(value))
                                                return true;
                                        }
                                }
                            }
                            break;
                    }
                }
                catch (Exception exc)
                {
                    //try-catch to ensure that no exception is throw
                    Debug.WriteLine(exc.Message);
                }
            }
            return false;
        }
    }

    public enum FormValueRequirement
    {
        Equal,
        StartsWith
    }
}
