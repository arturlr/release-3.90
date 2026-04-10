using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Web.Models.Customer;

namespace Nop.Web.Controllers;

public partial class CustomerController
{
    [HttpGet]
    public IActionResult Login(bool? checkoutAsGuest)
    {
        return View(new LoginModel
        {
            UsernamesEnabled = customerSettings.UsernamesEnabled,
            CheckoutAsGuest = checkoutAsGuest ?? false,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl)
    {
        if (ModelState.IsValid)
        {
            var usernameOrEmail = customerSettings.UsernamesEnabled
                ? model.Username?.Trim() ?? ""
                : model.Email ?? "";

            var loginResult = await customerRegistrationService.ValidateCustomerAsync(usernameOrEmail, model.Password ?? "");

            switch (loginResult)
            {
                case CustomerLoginResults.Successful:
                {
                    var customer = customerSettings.UsernamesEnabled
                        ? await customerService.GetCustomerByUsernameAsync(model.Username!)
                        : await customerService.GetCustomerByEmailAsync(model.Email!);

                    await shoppingCartService.MigrateShoppingCartAsync(workContext.CurrentCustomer, customer!, true);
                    await authenticationService.SignInAsync(customer!, model.RememberMe);
                    await eventPublisher.PublishAsync(new CustomerLoggedinEvent(customer!));
                    customerActivityService.InsertActivity(customer!, "PublicStore.Login",
                        await localizationService.GetResourceAsync("ActivityLog.PublicStore.Login"));

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Home");
                }
                case CustomerLoginResults.CustomerNotExist:
                    ModelState.AddModelError("", await localizationService.GetResourceAsync("Account.Login.WrongCredentials.CustomerNotExist"));
                    break;
                case CustomerLoginResults.Deleted:
                    ModelState.AddModelError("", await localizationService.GetResourceAsync("Account.Login.WrongCredentials.Deleted"));
                    break;
                case CustomerLoginResults.NotActive:
                    ModelState.AddModelError("", await localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotActive"));
                    break;
                case CustomerLoginResults.NotRegistered:
                    ModelState.AddModelError("", await localizationService.GetResourceAsync("Account.Login.WrongCredentials.NotRegistered"));
                    break;
                case CustomerLoginResults.LockedOut:
                    ModelState.AddModelError("", await localizationService.GetResourceAsync("Account.Login.WrongCredentials.LockedOut"));
                    break;
                default:
                    ModelState.AddModelError("", await localizationService.GetResourceAsync("Account.Login.WrongCredentials"));
                    break;
            }
        }

        model.UsernamesEnabled = customerSettings.UsernamesEnabled;
        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        if (workContext.OriginalCustomerIfImpersonated != null)
        {
            await genericAttributeService.SaveAttributeAsync<int?>(
                workContext.OriginalCustomerIfImpersonated,
                SystemCustomerAttributeNames.ImpersonatedCustomerId, null);
            return RedirectToAction("Edit", "Customer", new { id = workContext.CurrentCustomer.Id, area = "Admin" });
        }

        customerActivityService.InsertActivity("PublicStore.Logout",
            await localizationService.GetResourceAsync("ActivityLog.PublicStore.Logout"));

        await authenticationService.SignOutAsync();
        await eventPublisher.PublishAsync(new CustomerLoggedOutEvent(workContext.CurrentCustomer));

        return RedirectToAction("Index", "Home");
    }
}
