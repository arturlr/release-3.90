using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Web.Models.Customer;

namespace Nop.Web.Controllers;

public partial class CustomerController
{
    [HttpGet]
    public IActionResult PasswordRecovery()
    {
        return View(new PasswordRecoveryModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PasswordRecovery(PasswordRecoveryModel model)
    {
        if (ModelState.IsValid)
        {
            var customer = await customerService.GetCustomerByEmailAsync(model.Email ?? "");
            if (customer is { Active: true, Deleted: false })
            {
                await genericAttributeService.SaveAttributeAsync(customer,
                    SystemCustomerAttributeNames.PasswordRecoveryToken, Guid.NewGuid().ToString());
                await genericAttributeService.SaveAttributeAsync(customer,
                    SystemCustomerAttributeNames.PasswordRecoveryTokenDateGenerated, DateTime.UtcNow);
                await workflowMessageService.SendCustomerPasswordRecoveryMessageAsync(customer, workContext.WorkingLanguage.Id);
                model.Result = await localizationService.GetResourceAsync("Account.PasswordRecovery.EmailHasBeenSent");
            }
            else
            {
                model.Result = await localizationService.GetResourceAsync("Account.PasswordRecovery.EmailNotFound");
            }
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PasswordRecoveryConfirm(string token, string email)
    {
        var customer = await customerService.GetCustomerByEmailAsync(email);
        if (customer == null)
            return RedirectToAction("Index", "Home");

        var savedToken = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.PasswordRecoveryToken, genericAttributeService);
        if (string.IsNullOrEmpty(savedToken))
            return View(new PasswordRecoveryConfirmModel { DisablePasswordChanging = true, Result = "Password already changed." });

        var model = new PasswordRecoveryConfirmModel();

        if (!savedToken.Equals(token, StringComparison.OrdinalIgnoreCase))
        {
            model.DisablePasswordChanging = true;
            model.Result = await localizationService.GetResourceAsync("Account.PasswordRecovery.WrongToken");
        }

        // Check token expiration
        var generatedDate = await customer.GetAttributeAsync<DateTime?>(
            SystemCustomerAttributeNames.PasswordRecoveryTokenDateGenerated, genericAttributeService);
        if (generatedDate.HasValue && customerSettings.PasswordRecoveryLinkDaysValid > 0 &&
            generatedDate.Value.AddDays(customerSettings.PasswordRecoveryLinkDaysValid) < DateTime.UtcNow)
        {
            model.DisablePasswordChanging = true;
            model.Result = await localizationService.GetResourceAsync("Account.PasswordRecovery.LinkExpired");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PasswordRecoveryConfirm(string token, string email, PasswordRecoveryConfirmModel model)
    {
        var customer = await customerService.GetCustomerByEmailAsync(email);
        if (customer == null)
            return RedirectToAction("Index", "Home");

        var savedToken = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.PasswordRecoveryToken, genericAttributeService);
        if (string.IsNullOrEmpty(savedToken) || !savedToken.Equals(token, StringComparison.OrdinalIgnoreCase))
        {
            model.DisablePasswordChanging = true;
            model.Result = "Invalid token.";
            return View(model);
        }

        if (ModelState.IsValid)
        {
            var response = await customerRegistrationService.ChangePasswordAsync(new ChangePasswordRequest(
                email, false, customerSettings.DefaultPasswordFormat, model.NewPassword ?? ""));
            if (response.Success)
            {
                await genericAttributeService.SaveAttributeAsync(customer,
                    SystemCustomerAttributeNames.PasswordRecoveryToken, "");
                model.DisablePasswordChanging = true;
                model.Result = await localizationService.GetResourceAsync("Account.PasswordRecovery.PasswordHasBeenChanged");
            }
            else
            {
                model.Result = response.Errors.FirstOrDefault();
            }
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();
        return View(new ChangePasswordModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();

        if (ModelState.IsValid)
        {
            var result = await customerRegistrationService.ChangePasswordAsync(new ChangePasswordRequest(
                workContext.CurrentCustomer.Email!, true,
                customerSettings.DefaultPasswordFormat, model.NewPassword ?? "", model.OldPassword ?? ""));
            if (result.Success)
            {
                model.Result = await localizationService.GetResourceAsync("Account.ChangePassword.Success");
                return View(model);
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Avatar()
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();
        if (!customerSettings.AllowCustomersToUploadAvatars)
            return RedirectToAction("Info");

        var avatarPictureId = await workContext.CurrentCustomer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.AvatarPictureId, genericAttributeService);
        return View(new CustomerAvatarModel
        {
            AvatarUrl = await pictureService.GetPictureUrlAsync(avatarPictureId, mediaSettings.AvatarPictureSize, false),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile? uploadedFile)
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();
        if (!customerSettings.AllowCustomersToUploadAvatars)
            return RedirectToAction("Info");

        var customer = workContext.CurrentCustomer;
        var avatarPictureId = await customer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.AvatarPictureId, genericAttributeService);

        if (uploadedFile is { Length: > 0 })
        {
            if (uploadedFile.Length > customerSettings.AvatarMaximumSizeBytes)
            {
                ModelState.AddModelError("", $"Maximum avatar size is {customerSettings.AvatarMaximumSizeBytes} bytes.");
                return View("Avatar", new CustomerAvatarModel
                {
                    AvatarUrl = await pictureService.GetPictureUrlAsync(avatarPictureId, mediaSettings.AvatarPictureSize, false),
                });
            }

            using var ms = new MemoryStream();
            await uploadedFile.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var existingPicture = await pictureService.GetPictureByIdAsync(avatarPictureId);
            if (existingPicture != null)
                existingPicture = await pictureService.UpdatePictureAsync(existingPicture.Id, bytes, uploadedFile.ContentType, null);
            else
                existingPicture = await pictureService.InsertPictureAsync(bytes, uploadedFile.ContentType, null);

            avatarPictureId = existingPicture?.Id ?? 0;
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.AvatarPictureId, avatarPictureId);
        }

        return View("Avatar", new CustomerAvatarModel
        {
            AvatarUrl = await pictureService.GetPictureUrlAsync(avatarPictureId, mediaSettings.AvatarPictureSize, false),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAvatar()
    {
        if (!await IsRegisteredAsync(workContext.CurrentCustomer))
            return Challenge();
        if (!customerSettings.AllowCustomersToUploadAvatars)
            return RedirectToAction("Info");

        var customer = workContext.CurrentCustomer;
        var avatarPictureId = await customer.GetAttributeAsync<int>(
            SystemCustomerAttributeNames.AvatarPictureId, genericAttributeService);
        var picture = await pictureService.GetPictureByIdAsync(avatarPictureId);
        if (picture != null)
            await pictureService.DeletePictureAsync(picture);
        await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.AvatarPictureId, 0);

        return RedirectToAction("Avatar");
    }

    [HttpGet]
    public async Task<IActionResult> EmailRevalidation(string token, string email)
    {
        var customer = await customerService.GetCustomerByEmailAsync(email);
        if (customer == null)
            return RedirectToAction("Index", "Home");

        var cToken = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.EmailRevalidationToken, genericAttributeService);
        if (string.IsNullOrEmpty(cToken))
            return View(new EmailRevalidationModel { Result = "Email already changed." });

        if (!cToken.Equals(token, StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Home");

        if (string.IsNullOrEmpty(customer.EmailToRevalidate))
            return RedirectToAction("Index", "Home");

        try
        {
            await customerRegistrationService.SetEmailAsync(customer, customer.EmailToRevalidate, false);
        }
        catch (Exception exc)
        {
            return View(new EmailRevalidationModel { Result = exc.Message });
        }

        customer.EmailToRevalidate = null;
        await customerService.UpdateCustomerAsync(customer);
        await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.EmailRevalidationToken, "");

        if (!customerSettings.UsernamesEnabled)
            await authenticationService.SignInAsync(customer, true);

        return View(new EmailRevalidationModel { Result = "Your email address has been changed." });
    }
}
