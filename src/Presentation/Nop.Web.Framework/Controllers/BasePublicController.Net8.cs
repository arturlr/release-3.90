using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Base public controller
    /// </summary>
    public abstract class BasePublicController : BaseController
    {
        protected readonly IWorkContext _workContext;

        protected BasePublicController(IWorkContext workContext)
        {
            _workContext = workContext;
        }

        /// <summary>
        /// Get current customer
        /// </summary>
        protected virtual Customer CurrentCustomer => _workContext.CurrentCustomer;

        /// <summary>
        /// Get current language ID
        /// </summary>
        protected virtual int WorkingLanguageId => _workContext.WorkingLanguage?.Id ?? 0;
    }
}
