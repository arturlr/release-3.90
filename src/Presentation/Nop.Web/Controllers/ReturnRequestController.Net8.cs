using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class ReturnRequestController : BasePublicController
    {
        public ReturnRequestController(IWorkContext workContext) : base(workContext)
        {
        }

        // GET: /ReturnRequest
        public IActionResult CustomerReturnRequests()
        {
            return Content("Your Return Requests");
        }

        // GET: /ReturnRequest/ReturnRequest/5
        public IActionResult ReturnRequest(int orderId)
        {
            return Content($"Return Request for Order #{orderId}");
        }

        // POST: /ReturnRequest/ReturnRequest
        [HttpPost]
        public IActionResult ReturnRequest(int orderId, int orderItemId, int quantity, string reason)
        {
            // TODO: Create return request
            return Content("Return request submitted");
        }
    }
}
