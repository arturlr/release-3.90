using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Menu
{
    public class SiteMapNode
    {
        public SiteMapNode()
        {
            RouteValues = new RouteValueDictionary();
            ChildNodes = new List<SiteMapNode>();
        }

        public string SystemName { get; set; }
        public string Title { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public RouteValueDictionary RouteValues { get; set; }
        public string Url { get; set; }
        public IList<SiteMapNode> ChildNodes { get; set; }
        public string IconClass { get; set; }
        public bool Visible { get; set; }
        public bool OpenUrlInNewTab { get; set; }
    }
}
