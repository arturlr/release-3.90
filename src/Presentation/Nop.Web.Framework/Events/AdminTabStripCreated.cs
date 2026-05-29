using System.Collections.Generic;
using Microsoft.AspNetCore.Html;

namespace Nop.Web.Framework.Events
{
    /// <summary>
    /// Admin tabstrip created event
    /// </summary>
    public class AdminTabStripCreated
    {
        public AdminTabStripCreated(string tabStripName)
        {
            this.TabStripName = tabStripName;
            this.BlocksToRender = new List<IHtmlContent>();
        }

        public string TabStripName { get; private set; }
        public IList<IHtmlContent> BlocksToRender { get; set; }
    }
}
