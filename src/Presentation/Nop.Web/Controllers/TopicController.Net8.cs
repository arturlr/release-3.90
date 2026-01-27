using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class TopicController : BasePublicController
    {
        public TopicController(IWorkContext workContext) : base(workContext)
        {
        }

        // GET: /Topic/TopicDetails/about-us
        public IActionResult TopicDetails(string systemName)
        {
            return View((object)systemName);
        }

        // GET: /t/about-us (SEO friendly)
        [Route("/t/{systemName}")]
        public IActionResult TopicDetailsSeo(string systemName)
        {
            return TopicDetails(systemName);
        }
    }
}
