using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Models.Customer
{
    public partial class ExternalAuthenticationMethodModel : BaseNopModel
    {
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public RouteValueDictionary RouteValues { get; set; }
    }
}