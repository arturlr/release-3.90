using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Web.Factories;
using Nop.Web.Framework.Security;
using Nop.Web.Models.Order;

namespace Nop.Web.Controllers
{
    public partial class ReturnRequestController : BasePublicController
    {
        #region Fields

        private readonly IReturnRequestModelFactory _returnRequestModelFactory;
        private readonly IReturnRequestService _returnRequestService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerService _customerService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly ICustomNumberFormatter _customNumberFormatter;
        private readonly IDownloadService _downloadService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly OrderSettings _orderSettings;

        #endregion

        #region Constructors

        public ReturnRequestController(IReturnRequestModelFactory returnRequestModelFactory,
            IReturnRequestService returnRequestService,
            IOrderService orderService, 
            IWorkContext workContext, 
            IStoreContext storeContext,
            IOrderProcessingService orderProcessingService,
            ILocalizationService localizationService,
            ICustomerService customerService,
            IWorkflowMessageService workflowMessageService,
            ICustomNumberFormatter customNumberFormatter,
            IDownloadService downloadService,
            LocalizationSettings localizationSettings,
            OrderSettings orderSettings)
        {
            this._returnRequestModelFactory = returnRequestModelFactory;
            this._returnRequestService = returnRequestService;
            this._orderService = orderService;
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._orderProcessingService = orderProcessingService;
            this._localizationService = localizationService;
            this._customerService = customerService;
            this._workflowMessageService = workflowMessageService;
            this._customNumberFormatter = customNumberFormatter;
            this._downloadService = downloadService;
            this._localizationSettings = localizationSettings;
            this._orderSettings = orderSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Read a request value the way <c>System.Web.HttpRequest</c>'s indexer did.
        /// </summary>
        /// <remarks>
        /// Task 7.3. <c>Microsoft.AspNetCore.Http.HttpRequest</c> has no indexer. MVC 5's
        /// searched QueryString, then Form, then Cookies, then ServerVariables; only the first
        /// two are reachable for the call sites this replaces, and the form is guarded because
        /// ASP.NET Core throws on <c>Request.Form</c> for a non-form request.
        /// </remarks>
        /// <param name="key">Value name</param>
        /// <returns>Value, or null when absent</returns>
        protected virtual string GetRequestValue(string key)
        {
            if (Request.Query.ContainsKey(key))
                return Request.Query[key];

            if (Request.HasFormContentType && Request.Form.ContainsKey(key))
                return Request.Form[key];

            return null;
        }

        #endregion

        #region Methods

        [NopHttpsRequirement(SslRequirement.Yes)]
        public virtual ActionResult CustomerReturnRequests()
        {
            if (!_workContext.CurrentCustomer.IsRegistered())
                return new ChallengeResult();

            var model = _returnRequestModelFactory.PrepareCustomerReturnRequestsModel();
            return View(model);
        }

        [NopHttpsRequirement(SslRequirement.Yes)]
        public virtual ActionResult ReturnRequest(int orderId)
        {
            var order = _orderService.GetOrderById(orderId);
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return new ChallengeResult();

            if (!_orderProcessingService.IsReturnRequestAllowed(order))
                return RedirectToRoute("HomePage");

            var model = new SubmitReturnRequestModel();
            model = _returnRequestModelFactory.PrepareSubmitReturnRequestModel(model, order);
            return View(model);
        }

        [HttpPost, ActionName("ReturnRequest")]
        [PublicAntiForgery]
        public virtual ActionResult ReturnRequestSubmit(int orderId, SubmitReturnRequestModel model, IFormCollection form)
        {
            var order = _orderService.GetOrderById(orderId);
            if (order == null || order.Deleted || _workContext.CurrentCustomer.Id != order.CustomerId)
                return new ChallengeResult();

            if (!_orderProcessingService.IsReturnRequestAllowed(order))
                return RedirectToRoute("HomePage");

            int count = 0;

            var downloadId = 0;
            if (_orderSettings.ReturnRequestsAllowFiles)
            {
                var download = _downloadService.GetDownloadByGuid(model.UploadedFileGuid);
                if (download != null)
                    downloadId = download.Id;
            }

            //returnable products
            var orderItems = order.OrderItems.Where(oi => !oi.Product.NotReturnable);
            foreach (var orderItem in orderItems)
            {
                int quantity = 0; //parse quantity
                foreach (string formKey in form.Keys)
                    if (formKey.Equals(string.Format("quantity{0}", orderItem.Id), StringComparison.InvariantCultureIgnoreCase))
                    {
                        int.TryParse(form[formKey], out quantity);
                        break;
                    }
                if (quantity > 0)
                {
                    var rrr = _returnRequestService.GetReturnRequestReasonById(model.ReturnRequestReasonId);
                    var rra = _returnRequestService.GetReturnRequestActionById(model.ReturnRequestActionId);
                    
                    var rr = new ReturnRequest
                    {
                        CustomNumber = "",
                        StoreId = _storeContext.CurrentStore.Id,
                        OrderItemId = orderItem.Id,
                        Quantity = quantity,
                        CustomerId = _workContext.CurrentCustomer.Id,
                        ReasonForReturn = rrr != null ? rrr.GetLocalized(x => x.Name) : "not available",
                        RequestedAction = rra != null ? rra.GetLocalized(x => x.Name) : "not available",
                        CustomerComments = model.Comments,
                        UploadedFileId = downloadId,
                        StaffNotes = string.Empty,
                        ReturnRequestStatus = ReturnRequestStatus.Pending,
                        CreatedOnUtc = DateTime.UtcNow,
                        UpdatedOnUtc = DateTime.UtcNow
                    };
                    _workContext.CurrentCustomer.ReturnRequests.Add(rr);
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                    //set return request custom number
                    rr.CustomNumber = _customNumberFormatter.GenerateReturnRequestCustomNumber(rr);
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                    //notify store owner
                    _workflowMessageService.SendNewReturnRequestStoreOwnerNotification(rr, orderItem, _localizationSettings.DefaultAdminLanguageId);
                    //notify customer
                    _workflowMessageService.SendNewReturnRequestCustomerNotification(rr, orderItem, order.CustomerLanguageId);

                    count++;
                }
            }

            model = _returnRequestModelFactory.PrepareSubmitReturnRequestModel(model, order);
            if (count > 0)
                model.Result = _localizationService.GetResource("ReturnRequests.Submitted");
            else
                model.Result = _localizationService.GetResource("ReturnRequests.NoItemsSubmitted");

            return View(model);
        }

        [HttpPost]
        public virtual ActionResult UploadFileReturnRequest()
        {
            if (!_orderSettings.ReturnRequestsEnabled && !_orderSettings.ReturnRequestsAllowFiles)
            {
                return Json(new
                {
                    success = false,
                    downloadGuid = Guid.Empty,
                }, MimeTypes.TextPlain);
            }

            //we process it distinct ways based on a browser
            //find more info here http://stackoverflow.com/questions/4884920/mvc3-valums-ajax-file-upload
            Stream stream = null;
            var fileName = "";
            var contentType = "";
            //task 7.3: Request["qqfile"] -> the query string (then the form, matching MVC 5's
            //QueryString-before-Form search order). HttpRequest has no indexer in ASP.NET Core.
            var qqFile = GetRequestValue("qqfile");
            if (String.IsNullOrEmpty(qqFile))
            {
                // IE
                //Request.Files -> Request.Form.Files, guarded: ASP.NET Core throws on
                //Request.Form for a non-form request where System.Web returned an empty
                //collection.
                IFormFile httpPostedFile = Request.HasFormContentType && Request.Form.Files.Count > 0
                    ? Request.Form.Files[0]
                    : null;
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
                fileName = qqFile;
            }

            //task 7.3: 3.90 did "new byte[stream.Length]" then a single Read(). Request.Body is
            //not seekable in ASP.NET Core, so .Length throws NotSupportedException; and one
            //Read() is not guaranteed to fill the buffer even on a seekable stream. CopyTo
            //reads to completion - the same latent truncation bug task 4.2 fixed in
            //Nop.Services Media.Extensions.
            byte[] fileBinary;
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                fileBinary = ms.ToArray();
            }

            var fileExtension = Path.GetExtension(fileName);
            if (!String.IsNullOrEmpty(fileExtension))
                fileExtension = fileExtension.ToLowerInvariant();

            int validationFileMaximumSize = _orderSettings.ReturnRequestsFileMaximumSize;
            if (validationFileMaximumSize > 0)
            {
                //compare in bytes
                var maxFileSizeBytes = validationFileMaximumSize * 1024;
                if (fileBinary.Length > maxFileSizeBytes)
                {
                    //when returning JSON the mime-type must be set to text/plain
                    //otherwise some browsers will pop-up a "Save As" dialog.
                    return Json(new
                    {
                        success = false,
                        message = string.Format(_localizationService.GetResource("ShoppingCart.MaximumUploadedFileSize"), validationFileMaximumSize),
                        downloadGuid = Guid.Empty,
                    }, MimeTypes.TextPlain);
                }
            }

            var download = new Download
            {
                DownloadGuid = Guid.NewGuid(),
                UseDownloadUrl = false,
                DownloadUrl = "",
                DownloadBinary = fileBinary,
                ContentType = contentType,
                //we store filename without extension for downloads
                Filename = Path.GetFileNameWithoutExtension(fileName),
                Extension = fileExtension,
                IsNew = true
            };
            _downloadService.InsertDownload(download);

            //when returning JSON the mime-type must be set to text/plain
            //otherwise some browsers will pop-up a "Save As" dialog.
            return Json(new
            {
                success = true,
                message = _localizationService.GetResource("ShoppingCart.FileUploaded"),
                downloadUrl = Url.Action("GetFileUpload", "Download", new {downloadId = download.DownloadGuid}),
                downloadGuid = download.DownloadGuid,
            }, MimeTypes.TextPlain);
        }

        #endregion
    }
}
