using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Web.Models.Customer;

namespace Nop.Web.Controllers;

public partial class CustomerController
{
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        if (customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            return RedirectToAction("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled });

        var model = new RegisterModel();
        await PrepareRegisterModelAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterModel model, string? returnUrl)
    {
        if (customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
            return RedirectToAction("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled });

        if (await IsRegisteredAsync(workContext.CurrentCustomer))
        {
            await authenticationService.SignOutAsync();
            await eventPublisher.PublishAsync(new CustomerLoggedOutEvent(workContext.CurrentCustomer));
            workContext.CurrentCustomer = await customerService.InsertGuestCustomerAsync();
        }

        var customer = workContext.CurrentCustomer;
        customer.RegisteredInStoreId = storeContext.CurrentStore.Id;

        if (ModelState.IsValid)
        {
            if (customerSettings.UsernamesEnabled && model.Username != null)
                model.Username = model.Username.Trim();

            bool isApproved = customerSettings.UserRegistrationType == UserRegistrationType.Standard;
            var request = new CustomerRegistrationRequest(customer, model.Email ?? "",
                customerSettings.UsernamesEnabled ? model.Username ?? "" : model.Email ?? "",
                model.Password ?? "", customerSettings.DefaultPasswordFormat,
                storeContext.CurrentStore.Id, isApproved);

            var result = await customerRegistrationService.RegisterCustomerAsync(request);
            if (result.Success)
            {
                // Save generic attributes for form fields
                await SaveCustomerFormFieldsAsync(customer, model);

                // Newsletter
                if (customerSettings.NewsletterEnabled && model.Newsletter)
                    await EnsureNewsletterSubscriptionAsync(customer.Email!, storeContext.CurrentStore.Id);

                // Login if approved
                if (isApproved)
                    await authenticationService.SignInAsync(customer, true);

                // Default address
                await TryCreateDefaultAddressAsync(customer);

                // Notifications
                if (customerSettings.NotifyNewCustomerRegistration)
                    await workflowMessageService.SendCustomerRegisteredNotificationMessageAsync(customer, localizationSettings.DefaultAdminLanguageId);

                await eventPublisher.PublishAsync(new CustomerRegisteredEvent(customer));

                switch (customerSettings.UserRegistrationType)
                {
                    case UserRegistrationType.EmailValidation:
                        await genericAttributeService.SaveAttributeAsync(customer,
                            SystemCustomerAttributeNames.AccountActivationToken, Guid.NewGuid().ToString());
                        await workflowMessageService.SendCustomerEmailValidationMessageAsync(customer, workContext.WorkingLanguage.Id);
                        return RedirectToAction("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation });

                    case UserRegistrationType.AdminApproval:
                        return RedirectToAction("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval });

                    case UserRegistrationType.Standard:
                        await workflowMessageService.SendCustomerWelcomeMessageAsync(customer, workContext.WorkingLanguage.Id);
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                            return RedirectToAction("RegisterResult", new { resultId = (int)UserRegistrationType.Standard, returnUrl });
                        return RedirectToAction("RegisterResult", new { resultId = (int)UserRegistrationType.Standard });

                    default:
                        return RedirectToAction("Index", "Home");
                }
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);
        }

        await PrepareRegisterModelAsync(model);
        return View(model);
    }

    [HttpGet]
    public IActionResult RegisterResult(int resultId, string? returnUrl)
    {
        var resultText = ((UserRegistrationType)resultId) switch
        {
            UserRegistrationType.Disabled => "Registration is disabled.",
            UserRegistrationType.Standard => "Your registration completed.",
            UserRegistrationType.AdminApproval => "Your account will be activated after admin approval.",
            UserRegistrationType.EmailValidation => "Your registration completed. Check your email for validation instructions.",
            _ => ""
        };
        return View(new RegisterResultModel { Result = resultText, ReturnUrl = returnUrl });
    }

    [HttpPost]
    public IActionResult RegisterResult(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            return RedirectToAction("Index", "Home");
        return Redirect(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> CheckUsernameAvailability(string username)
    {
        var available = false;
        var statusText = await localizationService.GetResourceAsync("Account.CheckUsernameAvailability.NotAvailable");

        if (customerSettings.UsernamesEnabled && !string.IsNullOrWhiteSpace(username))
        {
            if (workContext.CurrentCustomer.Username != null &&
                workContext.CurrentCustomer.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
            {
                statusText = await localizationService.GetResourceAsync("Account.CheckUsernameAvailability.CurrentUsername");
            }
            else
            {
                var customer = await customerService.GetCustomerByUsernameAsync(username);
                if (customer == null)
                {
                    statusText = await localizationService.GetResourceAsync("Account.CheckUsernameAvailability.Available");
                    available = true;
                }
            }
        }

        return Json(new { Available = available, Text = statusText });
    }

    [HttpGet]
    public async Task<IActionResult> AccountActivation(string token, string email)
    {
        var customer = await customerService.GetCustomerByEmailAsync(email);
        if (customer == null)
            return RedirectToAction("Index", "Home");

        var cToken = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.AccountActivationToken, genericAttributeService);
        if (string.IsNullOrEmpty(cToken))
            return View(new AccountActivationModel { Result = "Your account is already activated." });

        if (!cToken.Equals(token, StringComparison.OrdinalIgnoreCase))
            return RedirectToAction("Index", "Home");

        customer.Active = true;
        await customerService.UpdateCustomerAsync(customer);
        await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.AccountActivationToken, "");
        await workflowMessageService.SendCustomerWelcomeMessageAsync(customer, workContext.WorkingLanguage.Id);

        return View(new AccountActivationModel { Result = "Your account has been activated." });
    }

    private async Task PrepareRegisterModelAsync(RegisterModel model)
    {
        model.UsernamesEnabled = customerSettings.UsernamesEnabled;
        model.CheckUsernameAvailabilityEnabled = customerSettings.CheckUsernameAvailabilityEnabled;
        model.GenderEnabled = customerSettings.GenderEnabled;
        model.DateOfBirthEnabled = customerSettings.DateOfBirthEnabled;
        model.DateOfBirthRequired = customerSettings.DateOfBirthRequired;
        model.CompanyEnabled = customerSettings.CompanyEnabled;
        model.CompanyRequired = customerSettings.CompanyRequired;
        model.StreetAddressEnabled = customerSettings.StreetAddressEnabled;
        model.StreetAddressRequired = customerSettings.StreetAddressRequired;
        model.StreetAddress2Enabled = customerSettings.StreetAddress2Enabled;
        model.StreetAddress2Required = customerSettings.StreetAddress2Required;
        model.ZipPostalCodeEnabled = customerSettings.ZipPostalCodeEnabled;
        model.ZipPostalCodeRequired = customerSettings.ZipPostalCodeRequired;
        model.CityEnabled = customerSettings.CityEnabled;
        model.CityRequired = customerSettings.CityRequired;
        model.CountryEnabled = customerSettings.CountryEnabled;
        model.CountryRequired = customerSettings.CountryRequired;
        model.StateProvinceEnabled = customerSettings.StateProvinceEnabled;
        model.StateProvinceRequired = customerSettings.StateProvinceRequired;
        model.PhoneEnabled = customerSettings.PhoneEnabled;
        model.PhoneRequired = customerSettings.PhoneRequired;
        model.FaxEnabled = customerSettings.FaxEnabled;
        model.FaxRequired = customerSettings.FaxRequired;
        model.NewsletterEnabled = customerSettings.NewsletterEnabled;
        model.EnteringEmailTwice = customerSettings.EnteringEmailTwice;
        model.AcceptPrivacyPolicyEnabled = customerSettings.AcceptPrivacyPolicyEnabled;
        model.DisplayVatNumber = taxSettings.EuVatEnabled;
        model.AllowCustomersToSetTimeZone = dateTimeSettings.AllowCustomersToSetTimeZone;

        if (customerSettings.CountryEnabled)
        {
            model.AvailableCountries.Add(new SelectListItem { Text = "Select country", Value = "0" });
            var countries = await countryService.GetAllCountriesAsync(workContext.WorkingLanguage.Id);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
        }

        if (dateTimeSettings.AllowCustomersToSetTimeZone)
        {
            foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
                model.AvailableTimeZones.Add(new SelectListItem { Text = tz.DisplayName, Value = tz.Id });
        }
    }

    private async Task SaveCustomerFormFieldsAsync(Customer customer, RegisterModel model)
    {
        if (dateTimeSettings.AllowCustomersToSetTimeZone)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.TimeZoneId, model.TimeZoneId);
        if (customerSettings.GenderEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.Gender, model.Gender);
        await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.FirstName, model.FirstName);
        await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.LastName, model.LastName);
        if (customerSettings.DateOfBirthEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.DateOfBirth, model.ParseDateOfBirth());
        if (customerSettings.CompanyEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.Company, model.Company);
        if (customerSettings.StreetAddressEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.StreetAddress, model.StreetAddress);
        if (customerSettings.StreetAddress2Enabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.StreetAddress2, model.StreetAddress2);
        if (customerSettings.ZipPostalCodeEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.ZipPostalCode, model.ZipPostalCode);
        if (customerSettings.CityEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.City, model.City);
        if (customerSettings.CountryEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.CountryId, model.CountryId);
        if (customerSettings.CountryEnabled && customerSettings.StateProvinceEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.StateProvinceId, model.StateProvinceId);
        if (customerSettings.PhoneEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.Phone, model.Phone);
        if (customerSettings.FaxEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.Fax, model.Fax);
        if (taxSettings.EuVatEnabled)
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.VatNumber, model.VatNumber);
    }

    private async Task EnsureNewsletterSubscriptionAsync(string email, int storeId)
    {
        var existing = await newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(email, storeId);
        if (existing != null)
        {
            existing.Active = true;
            await newsLetterSubscriptionService.UpdateNewsLetterSubscriptionAsync(existing);
        }
        else
        {
            await newsLetterSubscriptionService.InsertNewsLetterSubscriptionAsync(new NewsLetterSubscription
            {
                NewsLetterSubscriptionGuid = Guid.NewGuid(),
                Email = email,
                Active = true,
                StoreId = storeId,
                CreatedOnUtc = DateTime.UtcNow,
            });
        }
    }

    private async Task TryCreateDefaultAddressAsync(Customer customer)
    {
        var firstName = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.FirstName, genericAttributeService);
        var lastName = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.LastName, genericAttributeService);
        var countryId = await customer.GetAttributeAsync<int>(SystemCustomerAttributeNames.CountryId, genericAttributeService);
        var stateId = await customer.GetAttributeAsync<int>(SystemCustomerAttributeNames.StateProvinceId, genericAttributeService);

        var address = new Address
        {
            FirstName = firstName,
            LastName = lastName,
            Email = customer.Email,
            Company = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.Company, genericAttributeService),
            CountryId = countryId > 0 ? countryId : null,
            StateProvinceId = stateId > 0 ? stateId : null,
            City = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.City, genericAttributeService),
            Address1 = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.StreetAddress, genericAttributeService),
            Address2 = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.StreetAddress2, genericAttributeService),
            ZipPostalCode = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.ZipPostalCode, genericAttributeService),
            PhoneNumber = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.Phone, genericAttributeService),
            FaxNumber = await customer.GetAttributeAsync<string>(SystemCustomerAttributeNames.Fax, genericAttributeService),
            CreatedOnUtc = customer.CreatedOnUtc,
        };

        if (await addressService.IsAddressValidAsync(address))
        {
            await addressService.InsertAddressAsync(address);
            customerAddressRepository.Insert(new CustomerAddressMapping { CustomerId = customer.Id, AddressId = address.Id });
            customer.BillingAddressId = address.Id;
            customer.ShippingAddressId = address.Id;
            await customerService.UpdateCustomerAsync(customer);
        }
    }
}
