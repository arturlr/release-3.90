using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Tax;
using Nop.Services.Authentication;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers;

public partial class CustomerController(
    IAuthenticationService authenticationService,
    ICustomerService customerService,
    ICustomerRegistrationService customerRegistrationService,
    IGenericAttributeService genericAttributeService,
    ILocalizationService localizationService,
    IWorkContext workContext,
    IStoreContext storeContext,
    IEventPublisher eventPublisher,
    ICustomerActivityService customerActivityService,
    IWorkflowMessageService workflowMessageService,
    INewsLetterSubscriptionService newsLetterSubscriptionService,
    IAddressService addressService,
    ICountryService countryService,
    IShoppingCartService shoppingCartService,
    IPictureService pictureService,
    IRepository<CustomerAddressMapping> customerAddressRepository,
    CustomerSettings customerSettings,
    TaxSettings taxSettings,
    DateTimeSettings dateTimeSettings,
    LocalizationSettings localizationSettings,
    MediaSettings mediaSettings) : BasePublicController
{
    private async Task<bool> IsRegisteredAsync(Customer customer)
    {
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        return registeredRole != null && roleIds.Contains(registeredRole.Id);
    }
}
