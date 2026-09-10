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
    /// <remarks>
    /// Task 6.2: ported to ASP.NET Core.
    /// <list type="bullet">
    /// <item><c>System.Web.Mvc.ActionMethodSelectorAttribute.IsValidForRequest(ControllerContext,
    /// MethodInfo)</c> -&gt; <c>Microsoft.AspNetCore.Mvc.ActionMethodSelectorAttribute.IsValidForRequest(RouteContext,
    /// ActionDescriptor)</c>. This is a SIGNATURE CHANGE on a public virtual member: the
    /// <see cref="System.Reflection.MethodInfo"/> parameter is replaced by an
    /// <see cref="ActionDescriptor"/> and the controller context by a
    /// <see cref="RouteContext"/>. The method body never used the <c>MethodInfo</c>, so no
    /// behaviour is lost; in-tree subclasses: none.</item>
    /// <item><c>Request.Form.AllKeys</c> -&gt; <c>Request.Form.Keys</c>
    /// (<see cref="Microsoft.AspNetCore.Http.IFormCollection"/>).</item>
    /// <item>An explicit <c>HasFormContentType</c> guard was added. In System.Web reading
    /// <c>Request.Form</c> on a non-form request returned an empty collection; in ASP.NET Core it
    /// throws <c>InvalidOperationException</c>. The original relied on the surrounding
    /// try/catch (the "Invalid request exception" comment) - the guard keeps that intent while
    /// avoiding a thrown-and-swallowed exception on every GET.</item>
    /// </list>
    /// </remarks>
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
            if (routeContext == null || routeContext.HttpContext == null)
                return false;

            var request = routeContext.HttpContext.Request;
            if (request == null || !request.HasFormContentType)
                return false;

            foreach (string buttonName in _submitButtonNames)
            {
                try
                {
                    var form = request.Form;
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
                                    foreach (var formValue in form.Keys)
                                        if (formValue.StartsWith(buttonName, StringComparison.InvariantCultureIgnoreCase))
                                        { 
                                            var value = form[formValue];
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
