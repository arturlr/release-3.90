using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Media;
using Nop.Web.Framework.Security;
using Microsoft.AspNetCore.Http;

namespace Nop.Admin.Controllers
{
    public partial class PictureController : BaseAdminController
    {
        private readonly IPictureService _pictureService;

        public PictureController(IPictureService pictureService)
        {
            this._pictureService = pictureService;
        }

        [HttpPost]
        //do not validate request token (XSRF)
        [AdminAntiForgery(true)] 
        public virtual ActionResult AsyncUpload()
        {
            //if (!_permissionService.Authorize(StandardPermissionProvider.UploadPictures))
            //    return Json(new { success = false, error = "You do not have required permissions" }, "text/plain");

            //we process it distinct ways based on a browser
            //find more info here http://stackoverflow.com/questions/4884920/mvc3-valums-ajax-file-upload
            Stream stream = null;
            var fileName = "";
            var contentType = "";
            if (String.IsNullOrEmpty(GetRequestValue("qqfile")))
            {
                // IE
                IFormFile httpPostedFile = GetRequestFiles()[0];
                if (httpPostedFile == null)
                    throw new ArgumentException("No file uploaded");
                stream = httpPostedFile.OpenReadStream();
                fileName = Path.GetFileName(httpPostedFile.FileName);
                contentType = httpPostedFile.ContentType;
            }
            else
            {
                //Webkit, Mozilla
                stream = Request.Body;
                fileName = GetRequestValue("qqfile");
            }

            //TASK 8.3 - LATENT TRUNCATION BUG, FIXED. 3.90 did
            //  var fileBinary = new byte[stream.Length];
            //  stream.Read(fileBinary, 0, fileBinary.Length);
            //which is wrong twice over on ASP.NET Core. Request.Body is NOT SEEKABLE, so
            //stream.Length THROWS NotSupportedException on the Webkit/Mozilla branch above; and a
            //single Stream.Read is not guaranteed to fill the buffer even on a seekable stream, so
            //the IE branch could silently store a truncated file. CopyTo loops until the stream is
            //drained. This is the FOURTH time this exact bug has been found in this migration -
            //task 4.2 fixed it in Nop.Services' Media.Extensions.GetPictureBits/GetDownloadBits and
            //task 7.3 fixed it in Nop.Web's three valums-uploader blocks.
            byte[] fileBinary;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                fileBinary = ms.ToArray();
            }

            var fileExtension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();
            //contentType is not always available 
            //that's why we manually update it here
            //http://www.sfsu.edu/training/mimetype.htm
            if (String.IsNullOrEmpty(contentType))
            {
                switch (fileExtension)
                {
                    case ".bmp":
                        contentType = MimeTypes.ImageBmp;
                        break;
                    case ".gif":
                        contentType = MimeTypes.ImageGif;
                        break;
                    case ".jpeg":
                    case ".jpg":
                    case ".jpe":
                    case ".jfif":
                    case ".pjpeg":
                    case ".pjp":
                        contentType = MimeTypes.ImageJpeg;
                        break;
                    case ".png":
                        contentType = MimeTypes.ImagePng;
                        break;
                    case ".tiff":
                    case ".tif":
                        contentType = MimeTypes.ImageTiff;
                        break;
                    default:
                        break;
                }
            }

            var picture = _pictureService.InsertPicture(fileBinary, contentType, null);
            //when returning JSON the mime-type must be set to text/plain
            //otherwise some browsers will pop-up a "Save As" dialog.
            return Json(new { success = true, pictureId = picture.Id,
                imageUrl = _pictureService.GetPictureUrl(picture, 100) },
                MimeTypes.TextPlain);
        }
    }
}
