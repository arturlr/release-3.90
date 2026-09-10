using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Nop.Services.Payments;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Base controller for payment plugins
    /// </summary>
    /// <remarks>
    /// Task 6.2: PUBLIC SIGNATURE CHANGE. <c>System.Web.Mvc.FormCollection</c> -&gt;
    /// <see cref="IFormCollection"/> on both abstract members. ASP.NET Core does have a concrete
    /// <c>Microsoft.AspNetCore.Http.FormCollection</c>, but <c>HttpRequest.Form</c> is typed as
    /// <see cref="IFormCollection"/>, so keeping a concrete parameter type would force every
    /// caller to cast. The five Payments plugins (tasks 12.1-12.5) override these two methods and
    /// must change their parameter type accordingly; <c>form["key"]</c> indexing and
    /// <c>form.Keys</c> are otherwise source-compatible with the old
    /// <c>NameValueCollection</c>-derived <c>FormCollection</c> (note <c>AllKeys</c> -&gt;
    /// <c>Keys</c>, and the indexer yields <c>StringValues</c>, which converts implicitly to
    /// <see cref="string"/>).
    /// </remarks>
    public abstract class BasePaymentController : BasePluginController
    {
        public abstract IList<string> ValidatePaymentForm(IFormCollection form);
        public abstract ProcessPaymentRequest GetPaymentInfo(IFormCollection form);
    }
}
