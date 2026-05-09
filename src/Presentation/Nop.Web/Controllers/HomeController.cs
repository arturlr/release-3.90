using Nop.Web.Framework.Security;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Web.Controllers
{
    public partial class HomeController : BasePublicController
    {
        [NopHttpsRequirement(SslRequirement.No)]
        public virtual ActionResult Index()
        {
            return View();
        }
    }
}
