using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Routing;


namespace Nop.Admin.Models.Plugins
{
    public partial class MiscPluginModel : BaseNopModel
    {
        public string FriendlyName { get; set; }

        public string ConfigurationActionName { get; set; }
        public string ConfigurationControllerName { get; set; }
        public RouteValueDictionary ConfigurationRouteValues { get; set; }
    }
}