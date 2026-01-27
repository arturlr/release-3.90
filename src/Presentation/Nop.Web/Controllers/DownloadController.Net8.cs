using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class DownloadController : BasePublicController
    {
        public DownloadController(IWorkContext workContext) : base(workContext)
        {
        }

        // GET: /Download/Sample/5
        public IActionResult Sample(int productId)
        {
            // TODO: Get sample download
            return Content("Sample download");
        }

        // GET: /Download/GetDownload/guid
        public IActionResult GetDownload(Guid downloadGuid)
        {
            // TODO: Get purchased download
            return Content($"Download: {downloadGuid}");
        }

        // GET: /Download/GetLicense/guid
        public IActionResult GetLicense(Guid orderItemGuid)
        {
            // TODO: Get license file
            return Content($"License: {orderItemGuid}");
        }

        // GET: /Download/GetFileUpload/guid
        public IActionResult GetFileUpload(Guid downloadGuid)
        {
            // TODO: Get uploaded file
            return Content($"File: {downloadGuid}");
        }
    }
}
