using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Security;
using Nop.Web.Framework.Security;

namespace Nop.Admin.Controllers
{
    /// <summary>
    /// Controller used by jbimages (JustBoil.me) plugin (TimyMCE)
    /// </summary>
    //do not validate request token (XSRF)
    [AdminAntiForgery(true)]
    public partial class JbimagesController : BaseAdminController
    {
        private readonly IPermissionService _permissionService;

        public JbimagesController(IPermissionService permissionService)
        {
            this._permissionService = permissionService;
        }

        [NonAction]
        protected virtual IList<string> GetAllowedFileTypes()
        {
            return new List<string> {".gif", ".jpg", ".jpeg", ".png", ".bmp"};
        }

        [HttpPost]
        public virtual ActionResult Upload()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.HtmlEditorManagePictures))
            {
                ViewData["resultCode"] = "failed";
                ViewData["result"] = "No access to this functionality";
                return View();
            }

            if (GetRequestFiles().Count == 0)
                throw new Exception("No file uploaded");

            var uploadFile = GetRequestFiles()[0];
            if (uploadFile == null)
            {
                ViewData["resultCode"] = "failed";
                ViewData["result"] = "No file name provided";
                return View();
            }

            var fileName = Path.GetFileName(uploadFile.FileName);
            if (String.IsNullOrEmpty(fileName))
            {
                ViewData["resultCode"] = "failed";
                ViewData["result"] = "No file name provided";
                return View();
            }

            //TASK 8.5 - CASING FIX (runtime deferral 7.7-4, admin static-asset half).
            //Was "~/content/images/uploaded/". The directory on disk is Content/Images/uploaded,
            //so on a case-sensitive filesystem CommonHelper.MapPath returned a path whose parent
            //does not exist and the FileStream(..., FileMode.Create) below threw
            //DirectoryNotFoundException - i.e. uploading an image from the admin rich-text editor
            //failed outright on Linux/containers and worked on Windows. Same defect class task
            //7.7 found 8 instances of. Note Content/Roxy_Fileman/conf.json already spells its
            //FILES_ROOT "~/Content/Images/uploaded" correctly, so the two upload paths that share
            //this directory now agree.
            var directory = "~/Content/Images/uploaded/";
            var filePath = Path.Combine(CommonHelper.MapPath(directory), fileName);

            var fileExtension = Path.GetExtension(filePath);
            if (!GetAllowedFileTypes().Contains(fileExtension))
            {
                ViewData["resultCode"] = "failed";
                ViewData["result"] = string.Format("Files with {0} extension cannot be uploaded", fileExtension);
                return View();
            }

            //TASK 8.3 - HttpPostedFileBase.SaveAs -> IFormFile.CopyTo over a FileStream.
            //IFormFile has no SaveAs.
            using (var destStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                uploadFile.CopyTo(destStream);

            ViewData["resultCode"] = "success";
            ViewData["result"] = "success";
            ViewData["filename"] = this.Url.Content(string.Format("{0}{1}", directory, fileName));
            return View();
        }
    }
}
