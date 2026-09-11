using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace Nop.Plugin.Shipping.UPS
{
    /// <summary>
    /// Hand-built SOAP 1.1 client that replaces the WSDL-generated
    /// <c>System.Web.Services.Protocols.SoapHttpClientProtocol</c> TrackService proxy, which has no
    /// net10.0 counterpart.
    ///
    /// <para>
    /// TASK 14.5 — follows the 4.2 precedent and the 14.3 FedEx port. The DIFFERENCE from FedEx is
    /// that UPS sends a SOAP HEADER: the generated proxy carried <c>[SoapHeader("UPSSecurityValue")]</c>
    /// on <c>ProcessTrack</c>, so the <c>UPSSecurity</c> object is serialized into
    /// <c>&lt;soap:Header&gt;</c>, not the body. This invoker takes an optional header object and
    /// writes it there, reproducing the exact wire shape the proxy sent — an external contract with
    /// UPS. Recorded in runtime-deferrals.md §14.x-1.
    /// </para>
    ///
    /// <para>
    /// SOAP 1.1, document/literal, <c>ParameterStyle=Bare</c>: the request DTO is the single body
    /// element in the operation namespace; the header object is the single header element in its own
    /// root namespace; <c>text/xml; charset=utf-8</c> content type and a quoted <c>SOAPAction</c>
    /// HTTP header. A single static <see cref="HttpClient"/> is used. The call is synchronous because
    /// <c>IShipmentTracker.GetShipmentEvents</c> is synchronous, as 3.90's proxy Invoke was.
    /// </para>
    ///
    /// <para>
    /// NOTE the tracker sets
    /// <c>ServicePointManager.ServerCertificateValidationCallback += delegate { return true; }</c>
    /// before calling — that still compiles and, being process-global, affects HttpClient too, so the
    /// certificate behaviour 3.90 had is preserved without change here.
    /// </para>
    /// </summary>
    internal static class UpsSoapInvoker
    {
        private const string SoapEnvelopeNs = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string XsiNs = "http://www.w3.org/2001/XMLSchema-instance";
        private const string XsdNs = "http://www.w3.org/2001/XMLSchema";

        private static readonly HttpClient _httpClient = new HttpClient();

        public static TReply Invoke<THeader, TRequest, TReply>(
            string url,
            string soapAction,
            THeader header,
            string headerElementName,
            string headerNamespace,
            string requestElementName,
            string replyElementName,
            string operationNamespace,
            TRequest request)
        {
            //--- serialize the header (if any) as the bare header element ---
            var headerXml = new StringBuilder();
            if (header != null)
            {
                var headerSerializer = new XmlSerializer(typeof(THeader),
                    new XmlRootAttribute(headerElementName) { Namespace = headerNamespace });
                var writerSettings = new XmlWriterSettings { OmitXmlDeclaration = true, Encoding = new UTF8Encoding(false) };
                using (var sw = new StringWriter(headerXml))
                using (var xw = XmlWriter.Create(sw, writerSettings))
                {
                    var ns = new XmlSerializerNamespaces();
                    ns.Add("xsi", XsiNs);
                    ns.Add("xsd", XsdNs);
                    headerSerializer.Serialize(xw, header, ns);
                }
            }

            //--- serialize the request DTO as the bare body element ---
            var requestSerializer = new XmlSerializer(typeof(TRequest),
                new XmlRootAttribute(requestElementName) { Namespace = operationNamespace });
            var bodyXml = new StringBuilder();
            var bodyWriterSettings = new XmlWriterSettings { OmitXmlDeclaration = true, Encoding = new UTF8Encoding(false) };
            using (var sw = new StringWriter(bodyXml))
            using (var xw = XmlWriter.Create(sw, bodyWriterSettings))
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("xsi", XsiNs);
                ns.Add("xsd", XsdNs);
                requestSerializer.Serialize(xw, request, ns);
            }

            //--- wrap in a SOAP 1.1 envelope ---
            var envelope = new StringBuilder();
            envelope.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            envelope.Append("<soap:Envelope xmlns:soap=\"").Append(SoapEnvelopeNs).Append("\">");
            if (headerXml.Length > 0)
            {
                envelope.Append("<soap:Header>");
                envelope.Append(headerXml);
                envelope.Append("</soap:Header>");
            }
            envelope.Append("<soap:Body>");
            envelope.Append(bodyXml);
            envelope.Append("</soap:Body>");
            envelope.Append("</soap:Envelope>");

            //--- POST ---
            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, url))
            {
                var content = new StringContent(envelope.ToString(), new UTF8Encoding(false));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
                httpRequest.Content = content;
                httpRequest.Headers.TryAddWithoutValidation("SOAPAction", "\"" + soapAction + "\"");

                HttpResponseMessage httpResponse;
                try
                {
                    httpResponse = _httpClient.Send(httpRequest, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    throw new Exception("UPS SOAP request failed: " + ex.Message, ex);
                }

                string responseXml;
                using (var stream = httpResponse.Content.ReadAsStream())
                using (var reader = new StreamReader(stream))
                {
                    responseXml = reader.ReadToEnd();
                }

                var doc = new XmlDocument();
                doc.LoadXml(responseXml);
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("soap", SoapEnvelopeNs);

                var body = doc.SelectSingleNode("/soap:Envelope/soap:Body", nsmgr);
                if (body == null)
                    throw new Exception("UPS SOAP response had no Body element.");

                //--- SOAP fault? surface as SoapException with its <detail> node ---
                var fault = body.SelectSingleNode("soap:Fault", nsmgr);
                if (fault != null)
                {
                    var faultString = fault.SelectSingleNode("faultstring");
                    var detail = fault.SelectSingleNode("detail");
                    throw new SoapException(faultString != null ? faultString.InnerText : "SOAP fault", detail);
                }

                //--- deserialize the bare reply element ---
                XmlNode replyNode = null;
                foreach (XmlNode child in body.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element &&
                        string.Equals(child.LocalName, replyElementName, StringComparison.Ordinal))
                    {
                        replyNode = child;
                        break;
                    }
                }
                if (replyNode == null)
                {
                    foreach (XmlNode child in body.ChildNodes)
                    {
                        if (child.NodeType == XmlNodeType.Element)
                        {
                            replyNode = child;
                            break;
                        }
                    }
                }
                if (replyNode == null)
                    throw new Exception(string.Format("UPS SOAP response Body had no '{0}' element.", replyElementName));

                var replySerializer = new XmlSerializer(typeof(TReply),
                    new XmlRootAttribute(replyElementName) { Namespace = operationNamespace });
                using (var replyReader = new XmlNodeReader(replyNode))
                {
                    return (TReply)replySerializer.Deserialize(replyReader);
                }
            }
        }
    }
}
