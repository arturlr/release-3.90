using System;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;

namespace Nop.Web.Framework
{
    public class RssActionResult : ActionResult
    {
        public RssActionResult(SyndicationFeed feed, string feedPageUrl)
        {
            this.Feed = feed;
            XNamespace atom = "http://www.w3.org/2005/Atom";
            this.Feed.AttributeExtensions.Add(new XmlQualifiedName("atom", XNamespace.Xmlns.NamespaceName), atom.NamespaceName);
            this.Feed.ElementExtensions.Add(new XElement(atom + "link", new XAttribute("href", new Uri(feedPageUrl)), new XAttribute("rel", "self"), new XAttribute("type", "application/rss+xml")));
        }

        public SyndicationFeed Feed { get; set; }

        public override void ExecuteResult(ActionContext context)
        {
            context.HttpContext.Response.ContentType = MimeTypes.ApplicationRssXml;

            var rssFormatter = Feed.GetRss20Formatter();
            rssFormatter.SerializeExtensionsAsAtom = false;

            using (var writer = XmlWriter.Create(context.HttpContext.Response.Body, new XmlWriterSettings { Async = false }))
            {
                rssFormatter.WriteTo(writer);
            }
        }
    }
}
