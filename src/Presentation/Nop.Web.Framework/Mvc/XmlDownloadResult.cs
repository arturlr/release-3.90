using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Task 6.2: <c>System.Web.Mvc.ActionResult</c> -&gt;
    /// <see cref="Microsoft.AspNetCore.Mvc.ActionResult"/>.
    /// <list type="bullet">
    /// <item><c>ExecuteResult(ControllerContext)</c> -&gt;
    /// <c>ExecuteResultAsync(ActionContext)</c>.</item>
    /// <item><c>Response.Charset = "utf-8"</c> -&gt; folded into the content type
    /// (<c>text/xml; charset=utf-8</c>); ASP.NET Core has no separate <c>Charset</c>
    /// property.</item>
    /// <item><c>Response.AddHeader</c> -&gt; <c>Response.Headers["content-disposition"]</c>.</item>
    /// <item><c>Response.BinaryWrite(byte[])</c> -&gt; <c>Response.Body.WriteAsync</c>.</item>
    /// <item><c>Response.End()</c> was REMOVED - it has no ASP.NET Core equivalent and is not
    /// needed: an action result returns to the pipeline rather than aborting the request.
    /// In System.Web it also suppressed any further output; here nothing follows the result.</item>
    /// </list>
    /// </summary>
    public class XmlDownloadResult : ActionResult
    {
        public XmlDownloadResult(string xml, string fileDownloadName)
        {
            Xml = xml;
            FileDownloadName = fileDownloadName;
        }

        public string FileDownloadName
        {
            get;
            set;
        }

        public string Xml
        {
            get;
            set;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var document = new XmlDocument();
            document.LoadXml(Xml);
            var decl = document.FirstChild as XmlDeclaration;
            if (decl != null)
            {
                decl.Encoding = "utf-8";
            }

            var response = context.HttpContext.Response;
            response.ContentType = "text/xml; charset=utf-8";
            response.Headers["content-disposition"] = string.Format("attachment; filename={0}", FileDownloadName);

            var bytes = Encoding.UTF8.GetBytes(document.InnerXml);
            await response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
