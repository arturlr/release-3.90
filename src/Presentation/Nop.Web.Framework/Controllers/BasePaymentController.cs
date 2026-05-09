using System.Collections.Generic;
using Nop.Services.Payments;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Http;


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
