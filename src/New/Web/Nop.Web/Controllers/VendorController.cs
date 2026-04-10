using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Vendors;
using Nop.Core.Html;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Vendor;

namespace Nop.Web.Controllers;

public class VendorController(
    IWorkContext workContext,
    ICustomerService customerService,
    IVendorService vendorService,
    ILocalizationService localizationService,
    IWorkflowMessageService workflowMessageService,
    IUrlRecordService urlRecordService,
    IPictureService pictureService,
    VendorSettings vendorSettings,
    LocalizationSettings localizationSettings,
    SeoSettings seoSettings) : BasePublicController
{
    private async Task<bool> IsRegisteredAsync(Customer customer)
    {
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        return registeredRole != null && roleIds.Contains(registeredRole.Id);
    }

    // GET: /Vendor/ApplyVendor
    public async Task<IActionResult> ApplyVendor()
    {
        if (!vendorSettings.AllowCustomersToApplyForVendorAccount)
            return RedirectToAction("Index", "Home");

        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var model = new ApplyVendorModel();
        return View(model);
    }

    // POST: /Vendor/ApplyVendorSubmit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyVendorSubmit(ApplyVendorModel model, IFormFile? uploadedFile)
    {
        if (!vendorSettings.AllowCustomersToApplyForVendorAccount)
            return RedirectToAction("Index", "Home");

        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var pictureId = 0;
        if (uploadedFile is { Length: > 0 })
        {
            try
            {
                using var ms = new MemoryStream();
                await uploadedFile.CopyToAsync(ms);
                var picture = await pictureService.InsertPictureAsync(ms.ToArray(), uploadedFile.ContentType, null);
                pictureId = picture.Id;
            }
            catch
            {
                ModelState.AddModelError("", await localizationService.GetResourceAsync("Vendors.ApplyAccount.Picture.ErrorMessage"));
            }
        }

        if (ModelState.IsValid)
        {
            var description = HtmlHelper.FormatText(model.Description, false, false, true, false, false, false);
            var vendor = new Vendor
            {
                Name = model.Name,
                Email = model.Email,
                Description = description,
                PictureId = pictureId,
                PageSize = 6,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = vendorSettings.DefaultVendorPageSizeOptions
            };
            await vendorService.InsertVendorAsync(vendor);

            var seName = await vendor.ValidateSeNameAsync(vendor.Name, vendor.Name, true, urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(vendor, seName, 0);

            workContext.CurrentCustomer.VendorId = vendor.Id;
            await customerService.UpdateCustomerAsync(workContext.CurrentCustomer);

            if (pictureId > 0)
                await pictureService.SetSeoFilenameAsync(pictureId, pictureService.GetPictureSeName(vendor.Name ?? ""));

            await workflowMessageService.SendNewVendorAccountApplyStoreOwnerNotificationAsync(
                workContext.CurrentCustomer, vendor, localizationSettings.DefaultAdminLanguageId);

            model.DisableFormInput = true;
            model.Result = await localizationService.GetResourceAsync("Vendors.ApplyAccount.Submitted");
            return View("ApplyVendor", model);
        }

        return View("ApplyVendor", model);
    }

    // GET: /Vendor/Info
    public async Task<IActionResult> Info()
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var vendor = workContext.CurrentVendor;
        if (vendor == null || !vendorSettings.AllowVendorsToEditInfo)
            return RedirectToAction("Info", "Customer");

        var model = new VendorInfoModel
        {
            Name = vendor.Name,
            Email = vendor.Email,
            Description = vendor.Description,
            PictureUrl = await pictureService.GetPictureUrlAsync(vendor.PictureId)
        };
        return View(model);
    }

    // POST: /Vendor/InfoSave
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InfoSave(VendorInfoModel model, IFormFile? uploadedFile)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var vendor = workContext.CurrentVendor;
        if (vendor == null || !vendorSettings.AllowVendorsToEditInfo)
            return RedirectToAction("Info", "Customer");

        Picture? newPicture = null;
        if (uploadedFile is { Length: > 0 })
        {
            try
            {
                using var ms = new MemoryStream();
                await uploadedFile.CopyToAsync(ms);
                newPicture = await pictureService.InsertPictureAsync(ms.ToArray(), uploadedFile.ContentType, null);
            }
            catch
            {
                ModelState.AddModelError("", await localizationService.GetResourceAsync("Account.VendorInfo.Picture.ErrorMessage"));
            }
        }

        if (ModelState.IsValid)
        {
            vendor.Name = model.Name;
            vendor.Email = model.Email;
            vendor.Description = HtmlHelper.FormatText(model.Description, false, false, true, false, false, false);

            if (newPicture != null)
            {
                var prevPicture = await pictureService.GetPictureByIdAsync(vendor.PictureId);
                vendor.PictureId = newPicture.Id;
                if (prevPicture != null)
                    await pictureService.DeletePictureAsync(prevPicture);
            }

            await pictureService.SetSeoFilenameAsync(vendor.PictureId, pictureService.GetPictureSeName(vendor.Name ?? ""));
            await vendorService.UpdateVendorAsync(vendor);

            if (vendorSettings.NotifyStoreOwnerAboutVendorInformationChange)
                await workflowMessageService.SendVendorInformationChangeNotificationAsync(vendor, localizationSettings.DefaultAdminLanguageId);

            return RedirectToAction("Info");
        }

        model.PictureUrl = await pictureService.GetPictureUrlAsync(vendor.PictureId);
        return View("Info", model);
    }

    // POST: /Vendor/RemovePicture
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePicture()
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        var vendor = workContext.CurrentVendor;
        if (vendor == null || !vendorSettings.AllowVendorsToEditInfo)
            return RedirectToAction("Info", "Customer");

        var picture = await pictureService.GetPictureByIdAsync(vendor.PictureId);
        if (picture != null)
            await pictureService.DeletePictureAsync(picture);

        vendor.PictureId = 0;
        await vendorService.UpdateVendorAsync(vendor);

        if (vendorSettings.NotifyStoreOwnerAboutVendorInformationChange)
            await workflowMessageService.SendVendorInformationChangeNotificationAsync(vendor, localizationSettings.DefaultAdminLanguageId);

        return RedirectToAction("Info");
    }
}
