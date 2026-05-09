using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Routing;


namespace Nop.Admin.Models.Cms
{
    public partial class WidgetModel : BaseNopModel
    {
        [NopResourceDisplayName("Admin.ContentManagement.Widgets.Fields.FriendlyName")]

        public string FriendlyName { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Widgets.Fields.SystemName")]

        public string SystemName { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Widgets.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Widgets.Fields.IsActive")]
        public bool IsActive { get; set; }
        

        public string ConfigurationActionName { get; set; }
        public string ConfigurationControllerName { get; set; }
        public RouteValueDictionary ConfigurationRouteValues { get; set; }
    }
}