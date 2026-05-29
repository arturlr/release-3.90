using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Attribute to validate whether a certain form name (or value) was submitted
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class FormValueRequiredAttribute : ActionMethodSelectorAttribute
    {
        private readonly string[] _submitButtonNames;
        private readonly FormValueRequirement _requirement;
        private readonly bool _validateNameOnly;

        public FormValueRequiredAttribute(params string[] submitButtonNames) :
            this(FormValueRequirement.Equal, submitButtonNames)
        {
        }

        public FormValueRequiredAttribute(FormValueRequirement requirement, params string[] submitButtonNames) :
            this(requirement, true, submitButtonNames)
        {
        }

        public FormValueRequiredAttribute(FormValueRequirement requirement, bool validateNameOnly, params string[] submitButtonNames)
        {
            this._submitButtonNames = submitButtonNames;
            this._validateNameOnly = validateNameOnly;
            this._requirement = requirement;
        }

        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
            var request = routeContext.HttpContext.Request;
            if (!request.HasFormContentType)
                return false;

            var form = request.Form;

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
                                    if (form.Keys.Any(x => x.Equals(buttonName, StringComparison.InvariantCultureIgnoreCase)))
                                        return true;
                                }
                                else
                                {
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
                                    if (form.Keys.Any(x => x.StartsWith(buttonName, StringComparison.InvariantCultureIgnoreCase)))
                                        return true;
                                }
                                else
                                {
                                    foreach (var formKey in form.Keys)
                                        if (formKey.StartsWith(buttonName, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            var value = form[formKey];
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
