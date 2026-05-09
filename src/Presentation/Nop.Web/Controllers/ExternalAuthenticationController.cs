using Nop.Services.Authentication.External;
using Nop.Web.Factories;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Web.Controllers
{
    public partial class ExternalAuthenticationController : BasePublicController
    {
		#region Fields

        private readonly IExternalAuthenticationModelFactory _externalAuthenticationModelFactory;

        #endregion

        #region Ctor

        public ExternalAuthenticationController(IExternalAuthenticationModelFactory externalAuthenticationModelFactory)
        {
            this._externalAuthenticationModelFactory = externalAuthenticationModelFactory;
        }

        #endregion

        #region Methods

        public virtual RedirectResult RemoveParameterAssociation(string returnUrl)
        {
            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            ExternalAuthorizerHelper.RemoveParameters();
            return Redirect(returnUrl);
        }

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
        public virtual ActionResult ExternalMethods()
        {
            var model = _externalAuthenticationModelFactory.PrepareExternalMethodsModel();
            return PartialView(model);
        }

        #endregion
    }
}
