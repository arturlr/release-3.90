using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Web.Framework.Mvc
{
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
