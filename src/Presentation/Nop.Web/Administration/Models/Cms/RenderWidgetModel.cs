using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Cms
{
    public partial class RenderWidgetModel : BaseNopModel
    {
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { get; set; }
    }
}