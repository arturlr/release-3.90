using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Models.Cms
{
    public partial class RenderWidgetModel : BaseNopModel
    {
        public string ActionName { get; set; }
        public string ControllerName { get; set; }
        public RouteValueDictionary RouteValues { get; set; }
    }
}