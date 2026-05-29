using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Nop.Services.Payments;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Base controller for payment plugins
    /// </summary>
    public abstract class BasePaymentController : BasePluginController
    {
        public abstract IList<string> ValidatePaymentForm(IFormCollection form);
        public abstract ProcessPaymentRequest GetPaymentInfo(IFormCollection form);
    }
}
