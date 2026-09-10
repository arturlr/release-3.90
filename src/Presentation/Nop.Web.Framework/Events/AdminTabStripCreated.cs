using System.Collections.Generic;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Web.Framework.Events
{
    /// <summary>
    /// Admin tabstrip created event
    /// </summary>
    /// <remarks>
    /// Ported in task 6.3: <c>System.Web.Mvc.HtmlHelper</c> → <see cref="IHtmlHelper"/> and
    /// <c>IList&lt;MvcHtmlString&gt;</c> → <c>IList&lt;IHtmlContent&gt;</c>. Consumers that
    /// simply add to and enumerate <see cref="BlocksToRender"/> are unaffected; the ~30 admin
    /// <c>_CreateOrUpdate.cshtml</c> views construct this with <c>this.Html</c>, which is an
    /// <see cref="IHtmlHelper"/> in ASP.NET Core, and render each block with <c>@eventBlock</c>,
    /// which writes an <see cref="IHtmlContent"/> directly.
    /// Any plugin that adds a block must now produce an <see cref="IHtmlContent"/> — the usual
    /// source is <c>Html.Partial(...)</c>, which already returns one.
    /// </remarks>
    public class AdminTabStripCreated
    {
        public AdminTabStripCreated(IHtmlHelper helper, string tabStripName)
        {
            this.Helper = helper;
            this.TabStripName = tabStripName;
            this.BlocksToRender = new List<IHtmlContent>();
        }

        public IHtmlHelper Helper { get; private set; }
        public string TabStripName { get; private set; }
        public IList<IHtmlContent> BlocksToRender { get; set; }
    }
}
