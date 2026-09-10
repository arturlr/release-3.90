using System;
using System.IO;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Task 6.2: <c>System.Web.Mvc.ActionResult</c> -&gt;
    /// <see cref="Microsoft.AspNetCore.Mvc.ActionResult"/>.
    /// <list type="bullet">
    /// <item><c>ExecuteResult(ControllerContext)</c> -&gt;
    /// <c>ExecuteResultAsync(ActionContext)</c>.</item>
    /// <item><c>Response.Output</c> (a <see cref="TextWriter"/>) has no ASP.NET Core equivalent -
    /// <c>HttpResponse</c> exposes only a <see cref="Stream"/>. The feed is written into a
    /// <see cref="StringWriter"/> and the result copied to the body, which also keeps the
    /// <see cref="XmlWriter"/> fully flushed before anything is sent.</item>
    /// <item>The charset is now declared on the content type; ASP.NET Core has no
    /// <c>Response.Charset</c>. UTF-8 was already what System.Web emitted by default.</item>
    /// </list>
    /// The feed content itself - <see cref="SyndicationFeed"/>, the atom namespace attribute and
    /// the <c>atom:link rel="self"</c> element, and <c>SerializeExtensionsAsAtom = false</c> -
    /// is unchanged; <c>System.ServiceModel.Syndication</c> is supplied by the package added in
    /// task 6.1.
    /// </summary>
    public class RssActionResult : ActionResult
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="feed">Syndication feed</param>
        /// <param name="feedPageUrl">Feed page url for atom self link</param>
        public RssActionResult(SyndicationFeed feed, string feedPageUrl)
        {
            this.Feed = feed;
            //add atom namespace
            XNamespace atom = "http://www.w3.org/2005/Atom";
            this.Feed.AttributeExtensions.Add(new XmlQualifiedName("atom", XNamespace.Xmlns.NamespaceName), atom.NamespaceName);
            //add atom:link with rel='self' 
            this.Feed.ElementExtensions.Add(new XElement(atom + "link", new XAttribute("href", new Uri(feedPageUrl)), new XAttribute("rel", "self"), new XAttribute("type", "application/rss+xml")));
        }
        public SyndicationFeed Feed { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = MimeTypes.ApplicationRssXml + "; charset=utf-8";

            var rssFormatter = Feed.GetRss20Formatter();
            //remove a10 namespace
            rssFormatter.SerializeExtensionsAsAtom = false;

            var sb = new StringBuilder();
            using (var stringWriter = new StringWriter(sb))
            using (var writer = XmlWriter.Create(stringWriter))
            {
                rssFormatter.WriteTo(writer);
            }

            return response.WriteAsync(sb.ToString(), Encoding.UTF8);
        }
    }
}
