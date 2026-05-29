using System;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;

namespace Nop.Web.Framework
{
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

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.ContentType = MimeTypes.ApplicationRssXml;

            var rssFormatter = Feed.GetRss20Formatter();
            rssFormatter.SerializeExtensionsAsAtom = false;

            using (var writer = XmlWriter.Create(response.Body, new XmlWriterSettings { Async = true }))
            {
                rssFormatter.WriteTo(writer);
                await writer.FlushAsync();
            }
        }
    }
}
