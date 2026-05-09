using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Web.Factories;
using Nop.Web.Framework.Security;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Web.Controllers
{
    [NopHttpsRequirement(SslRequirement.No)]
    public partial class ProfileController : BasePublicController
    {
        private readonly IProfileModelFactory _profileModelFactory;
        private readonly ICustomerService _customerService;
        private readonly IPermissionService _permissionService;
        private readonly CustomerSettings _customerSettings;

        public ProfileController(IProfileModelFactory profileModelFactory,
            ICustomerService customerService,
            IPermissionService permissionService,
            CustomerSettings customerSettings)
        {
            this._profileModelFactory = profileModelFactory;
            this._customerService = customerService;
            this._permissionService = permissionService;
            this._customerSettings = customerSettings;
        }

        public virtual ActionResult Index(int? id, int? page)
        {
            if (!_customerSettings.AllowViewingProfiles)
            {
                return RedirectToRoute("HomePage");
            }

            var customerId = 0;
            if (id.HasValue)
            {
                customerId = id.Value;
            }

            var customer = _customerService.GetCustomerById(customerId);
            if (customer == null || customer.IsGuest())
            {
                return RedirectToRoute("HomePage");
            }

            //display "edit" (manage) link
            if (_permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel) && _permissionService.Authorize(StandardPermissionProvider.ManageCustomers))
                DisplayEditLink(Url.Action("Edit", "Customer", new { id = customer.Id, area = "Admin" }));

            var model = _profileModelFactory.PrepareProfileIndexModel(customer, page);
            return View(model);
        }

        //profile info tab
        /* Added by CTA: This attribute is not available anymore. An alternative is using ViewComponents:
Sample:

public class SampleViewComponent : ViewComponent
    {
        private readonly InjectedService _injectedService;

        public SampleViewComponent (InjectedService injectedService)
        {
            _injectedService = injectedService;
        }


       public IViewComponentResult Invoke(int parameter)
        {
            var object = _injectedService.SampleFunction(parameter);
        // No name is specified, returns the view SampleView (same name as component)
            return View(object);
        }
    }

Then use this to call the view component from any view:

    @await Component.InvokeAsync("SampleView", new { parameter = ""})

https://docs.microsoft.com/en-us/aspnet/core/mvc/views/view-components?view=aspnetcore-3.1 */
        public virtual ActionResult Info(int customerProfileId)
        {
            var customer = _customerService.GetCustomerById(customerProfileId);
            if (customer == null)
            {
                return RedirectToRoute("HomePage");
            }

            var model = _profileModelFactory.PrepareProfileInfoModel(customer);
            return PartialView(model);
        }

        //latest posts tab
        /* Added by CTA: This attribute is not available anymore. An alternative is using ViewComponents:
Sample:

public class SampleViewComponent : ViewComponent
    {
        private readonly InjectedService _injectedService;

        public SampleViewComponent (InjectedService injectedService)
        {
            _injectedService = injectedService;
        }


       public IViewComponentResult Invoke(int parameter)
        {
            var object = _injectedService.SampleFunction(parameter);
        // No name is specified, returns the view SampleView (same name as component)
            return View(object);
        }
    }

Then use this to call the view component from any view:

    @await Component.InvokeAsync("SampleView", new { parameter = ""})

https://docs.microsoft.com/en-us/aspnet/core/mvc/views/view-components?view=aspnetcore-3.1 */
        public virtual ActionResult Posts(int customerProfileId, int page)
        {
            var customer = _customerService.GetCustomerById(customerProfileId);
            if (customer == null)
            {
                return RedirectToRoute("HomePage");
            }
            
            var model = _profileModelFactory.PrepareProfilePostsModel(customer, page);
            return PartialView(model);
        }
    }
}
