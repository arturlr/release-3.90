using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Security;

namespace Nop.Services.Customers;

public class CustomerRegistrationService : ICustomerRegistrationService
{
    private readonly ICustomerService _customerService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILocalizationService _localizationService;
    private readonly IEventPublisher _eventPublisher;
    private readonly CustomerSettings _customerSettings;

    public CustomerRegistrationService(
        ICustomerService customerService,
        IEncryptionService encryptionService,
        ILocalizationService localizationService,
        IEventPublisher eventPublisher,
        CustomerSettings customerSettings)
    {
        _customerService = customerService;
        _encryptionService = encryptionService;
        _localizationService = localizationService;
        _eventPublisher = eventPublisher;
        _customerSettings = customerSettings;
    }

    public virtual async Task<CustomerLoginResults> ValidateCustomerAsync(string usernameOrEmail, string password)
    {
        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsernameAsync(usernameOrEmail)
            : await _customerService.GetCustomerByEmailAsync(usernameOrEmail);

        if (customer == null)
            return CustomerLoginResults.CustomerNotExist;
        if (customer.Deleted)
            return CustomerLoginResults.Deleted;
        if (!customer.Active)
            return CustomerLoginResults.NotActive;

        // only registered can login
        var roleIds = await _customerService.GetCustomerRoleIdsAsync(customer);
        var registeredRole = await _customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        if (registeredRole == null || !roleIds.Contains(registeredRole.Id))
            return CustomerLoginResults.NotRegistered;

        // check lockout
        if (customer.CannotLoginUntilDateUtc.HasValue && customer.CannotLoginUntilDateUtc.Value > DateTime.UtcNow)
            return CustomerLoginResults.LockedOut;

        if (!PasswordsMatch(await _customerService.GetCurrentPasswordAsync(customer.Id), password))
        {
            customer.FailedLoginAttempts++;
            if (_customerSettings.FailedPasswordAllowedAttempts > 0 &&
                customer.FailedLoginAttempts >= _customerSettings.FailedPasswordAllowedAttempts)
            {
                customer.CannotLoginUntilDateUtc = DateTime.UtcNow.AddMinutes(_customerSettings.FailedPasswordLockoutMinutes);
                customer.FailedLoginAttempts = 0;
            }
            await _customerService.UpdateCustomerAsync(customer);
            return CustomerLoginResults.WrongPassword;
        }

        // successful login
        customer.FailedLoginAttempts = 0;
        customer.CannotLoginUntilDateUtc = null;
        customer.RequireReLogin = false;
        customer.LastLoginDateUtc = DateTime.UtcNow;
        await _customerService.UpdateCustomerAsync(customer);

        return CustomerLoginResults.Successful;
    }

    public virtual async Task<CustomerRegistrationResult> RegisterCustomerAsync(CustomerRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Customer);

        var result = new CustomerRegistrationResult();

        if (request.Customer.IsSystemAccount)
        {
            result.AddError("System account can't be registered");
            return result;
        }

        // check if already registered
        var roleIds = await _customerService.GetCustomerRoleIdsAsync(request.Customer, showHidden: true);
        var registeredRole = await _customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        if (registeredRole != null && roleIds.Contains(registeredRole.Id))
        {
            result.AddError("Current customer is already registered");
            return result;
        }

        if (string.IsNullOrEmpty(request.Email))
        {
            result.AddError(await _localizationService.GetResourceAsync("Account.Register.Errors.EmailIsNotProvided"));
            return result;
        }
        if (!CommonHelper.IsValidEmail(request.Email))
        {
            result.AddError(await _localizationService.GetResourceAsync("Common.WrongEmail"));
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            result.AddError(await _localizationService.GetResourceAsync("Account.Register.Errors.PasswordIsNotProvided"));
            return result;
        }
        if (_customerSettings.UsernamesEnabled && string.IsNullOrEmpty(request.Username))
        {
            result.AddError(await _localizationService.GetResourceAsync("Account.Register.Errors.UsernameIsNotProvided"));
            return result;
        }

        // validate unique email
        if (await _customerService.GetCustomerByEmailAsync(request.Email) != null)
        {
            result.AddError(await _localizationService.GetResourceAsync("Account.Register.Errors.EmailAlreadyExists"));
            return result;
        }

        // validate unique username
        if (_customerSettings.UsernamesEnabled && await _customerService.GetCustomerByUsernameAsync(request.Username) != null)
        {
            result.AddError(await _localizationService.GetResourceAsync("Account.Register.Errors.UsernameAlreadyExists"));
            return result;
        }

        // set customer properties
        request.Customer.Username = request.Username;
        request.Customer.Email = request.Email;

        // create password
        var customerPassword = new CustomerPassword
        {
            CustomerId = request.Customer.Id,
            PasswordFormat = request.PasswordFormat,
            CreatedOnUtc = DateTime.UtcNow
        };

        switch (request.PasswordFormat)
        {
            case PasswordFormat.Clear:
                customerPassword.Password = request.Password;
                break;
            case PasswordFormat.Encrypted:
                customerPassword.Password = _encryptionService.EncryptText(request.Password);
                break;
            case PasswordFormat.Hashed:
                var saltKey = _encryptionService.CreateSaltKey(5);
                customerPassword.PasswordSalt = saltKey;
                customerPassword.Password = _encryptionService.CreatePasswordHash(
                    request.Password, saltKey, _customerSettings.HashedPasswordFormat ?? "SHA1");
                break;
        }

        await _customerService.InsertCustomerPasswordAsync(customerPassword);

        request.Customer.Active = request.IsApproved;

        // add to 'Registered' role
        if (registeredRole == null)
            throw new NopException("'Registered' role could not be loaded");

        await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping
        {
            CustomerId = request.Customer.Id,
            CustomerRoleId = registeredRole.Id
        });

        // remove from 'Guests' role
        var guestRole = await _customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        if (guestRole != null)
            await _customerService.RemoveCustomerRoleMappingAsync(request.Customer, guestRole);

        // Reward points for registration deferred to [4.9] when IRewardPointService is built
        // Newsletter subscription deferred to [4.2] when INewsLetterSubscriptionService is built

        await _customerService.UpdateCustomerAsync(request.Customer);

        await _eventPublisher.PublishAsync(new CustomerPasswordChangedEvent(customerPassword));

        return result;
    }

    public virtual async Task<ChangePasswordResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new ChangePasswordResult();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.EmailIsNotProvided"));
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.PasswordIsNotProvided"));
            return result;
        }

        var customer = await _customerService.GetCustomerByEmailAsync(request.Email);
        if (customer == null)
        {
            result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.EmailNotFound"));
            return result;
        }

        if (request.ValidateRequest)
        {
            if (!PasswordsMatch(await _customerService.GetCurrentPasswordAsync(customer.Id), request.OldPassword))
            {
                result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.OldPasswordDoesntMatch"));
                return result;
            }
        }

        // check for duplicates
        if (_customerSettings.UnduplicatedPasswordsNumber > 0)
        {
            var previousPasswords = await _customerService.GetCustomerPasswordsAsync(
                customer.Id, passwordsToReturn: _customerSettings.UnduplicatedPasswordsNumber);

            if (previousPasswords.Any(p => PasswordsMatch(p, request.NewPassword)))
            {
                result.AddError(await _localizationService.GetResourceAsync("Account.ChangePassword.Errors.PasswordMatchesWithPrevious"));
                return result;
            }
        }

        var customerPassword = new CustomerPassword
        {
            CustomerId = customer.Id,
            PasswordFormat = request.NewPasswordFormat,
            CreatedOnUtc = DateTime.UtcNow
        };

        switch (request.NewPasswordFormat)
        {
            case PasswordFormat.Clear:
                customerPassword.Password = request.NewPassword;
                break;
            case PasswordFormat.Encrypted:
                customerPassword.Password = _encryptionService.EncryptText(request.NewPassword);
                break;
            case PasswordFormat.Hashed:
                var saltKey = _encryptionService.CreateSaltKey(5);
                customerPassword.PasswordSalt = saltKey;
                customerPassword.Password = _encryptionService.CreatePasswordHash(
                    request.NewPassword, saltKey, _customerSettings.HashedPasswordFormat ?? "SHA1");
                break;
        }

        await _customerService.InsertCustomerPasswordAsync(customerPassword);
        await _eventPublisher.PublishAsync(new CustomerPasswordChangedEvent(customerPassword));

        return result;
    }

    public virtual async Task SetEmailAsync(Customer customer, string newEmail, bool requireValidation)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(newEmail);

        newEmail = newEmail.Trim();

        if (!CommonHelper.IsValidEmail(newEmail))
            throw new NopException(await _localizationService.GetResourceAsync("Account.EmailUsernameErrors.NewEmailIsNotValid"));

        if (newEmail.Length > 100)
            throw new NopException(await _localizationService.GetResourceAsync("Account.EmailUsernameErrors.EmailTooLong"));

        var existing = await _customerService.GetCustomerByEmailAsync(newEmail);
        if (existing != null && customer.Id != existing.Id)
            throw new NopException(await _localizationService.GetResourceAsync("Account.EmailUsernameErrors.EmailAlreadyExists"));

        if (requireValidation)
        {
            customer.EmailToRevalidate = newEmail;
            await _customerService.UpdateCustomerAsync(customer);
            // Email revalidation message deferred to [4.2] when IWorkflowMessageService is built
        }
        else
        {
            customer.Email = newEmail;
            await _customerService.UpdateCustomerAsync(customer);
            // Newsletter subscription update deferred to [4.2] when INewsLetterSubscriptionService is built
        }
    }

    public virtual async Task SetUsernameAsync(Customer customer, string newUsername)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (!_customerSettings.UsernamesEnabled)
            throw new NopException("Usernames are disabled");

        newUsername = newUsername.Trim();

        if (newUsername.Length > 100)
            throw new NopException(await _localizationService.GetResourceAsync("Account.EmailUsernameErrors.UsernameTooLong"));

        var existing = await _customerService.GetCustomerByUsernameAsync(newUsername);
        if (existing != null && customer.Id != existing.Id)
            throw new NopException(await _localizationService.GetResourceAsync("Account.EmailUsernameErrors.UsernameAlreadyExists"));

        customer.Username = newUsername;
        await _customerService.UpdateCustomerAsync(customer);
    }

    protected bool PasswordsMatch(CustomerPassword? customerPassword, string enteredPassword)
    {
        if (customerPassword == null || string.IsNullOrEmpty(enteredPassword))
            return false;

        var savedPassword = customerPassword.PasswordFormat switch
        {
            PasswordFormat.Clear => enteredPassword,
            PasswordFormat.Encrypted => _encryptionService.EncryptText(enteredPassword),
            PasswordFormat.Hashed => _encryptionService.CreatePasswordHash(
                enteredPassword, customerPassword.PasswordSalt ?? string.Empty,
                _customerSettings.HashedPasswordFormat ?? "SHA1"),
            _ => string.Empty
        };

        return customerPassword.Password == savedPassword;
    }
}
