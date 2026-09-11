using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace Nop.Plugin.Shipping.Fedex
{
    /// <summary>
    /// Hand-built SOAP 1.1 client that replaces the WSDL-generated
    /// <c>System.Web.Services.Protocols.SoapHttpClientProtocol</c> proxies (RateService and
    /// TrackService), which have no net10.0 counterpart.
    ///
    /// <para>
    /// TASK 14.3 — this follows the 4.2 precedent EXACTLY: the EU VAT ASMX proxy was replaced by a
    /// hand-built SOAP envelope over <c>HttpClient</c> + XML serialization (runtime-deferrals.md
    /// §7.16 / §16). The DIFFERENCE is scale: FedEx's WSDL generated ~16,700 lines of
    /// <c>[XmlType]</c> DTO classes for RateService and ~4,850 for TrackService. Those DTOs are
    /// plain serializable types and compile unchanged on net10.0 — only the ONE
    /// <c>SoapHttpClientProtocol</c>-derived proxy class in each file is unportable, so only those
    /// two classes were replaced (by <c>RateService</c> and <c>TrackService</c> that call the
    /// invoker below); every DTO is kept verbatim.
    /// </para>
    ///
    /// <para>
    /// <b>THE WIRE SHAPE IS PRESERVED, because it is an external contract with FedEx.</b> The
    /// generated proxies used SOAP 1.1, document/literal, <c>ParameterStyle=Bare</c>, which puts
    /// the request DTO directly under <c>&lt;soap:Body&gt;</c> as a single element named for the
    /// message part (e.g. <c>RateRequest</c>) in the operation namespace. This invoker reproduces
    /// exactly that:
    /// <list type="bullet">
    /// <item>SOAP 1.1 envelope, <c>text/xml; charset=utf-8</c> content type, and a quoted
    /// <c>SOAPAction</c> HTTP header — the three things <c>SoapHttpClientProtocol</c> sent for
    /// SOAP 1.1.</item>
    /// <item>The request DTO is serialized with an <see cref="XmlSerializer"/> whose root is
    /// overridden to the bare element name and operation namespace — byte-for-byte what the
    /// <c>[return: XmlElement(...)]</c> / <c>[XmlElement(Namespace=...)]</c> attributes on the
    /// generated method produced.</item>
    /// <item><c>xsi</c>/<c>xsd</c> namespace declarations are emitted on the body element (as
    /// <c>SoapHttpClientProtocol</c> did) so <c>xsi:nil</c>/typed values serialize identically.</item>
    /// <item>A SOAP fault response is surfaced as this plugin's <see cref="SoapException"/> with
    /// its <c>&lt;detail&gt;</c> node, matching the two <c>catch (SoapException)</c> sites.</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// A single static <see cref="HttpClient"/> is used (the documented correct pattern, and the
    /// reason this migration keeps declining a naive per-call HttpClient elsewhere). The call is
    /// synchronous (<c>.GetAwaiter().GetResult()</c>) because both call sites are synchronous
    /// <c>IShippingRateComputationMethod.GetShippingOptions</c> / <c>IShipmentTracker</c> members
    /// that 3.90's <c>SoapHttpClientProtocol.Invoke</c> also ran synchronously.
    /// </para>
    /// </summary>
    internal static class FedexSoapInvoker
    {
        private const string SoapEnvelopeNs = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string XsiNs = "http://www.w3.org/2001/XMLSchema-instance";
        private const string XsdNs = "http://www.w3.org/2001/XMLSchema";

        //Documented correct pattern: one static HttpClient for the process. SoapHttpClientProtocol
        //pooled its connections through ServicePoint; a static HttpClient is the net10.0 analogue.
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Invokes a SOAP 1.1 document/literal/bare operation.
        /// </summary>
        /// <typeparam name="TRequest">Request DTO type (the bare body element)</typeparam>
        /// <typeparam name="TReply">Reply DTO type (the bare body element)</typeparam>
        /// <param name="url">Service endpoint URL</param>
        /// <param name="soapAction">Value for the SOAPAction HTTP header</param>
        /// <param name="requestElementName">Local name of the request body element (e.g. "RateRequest")</param>
        /// <param name="replyElementName">Local name of the reply body element (e.g. "RateReply")</param>
        /// <param name="operationNamespace">Operation XML namespace (e.g. "http://fedex.com/ws/rate/v16")</param>
        /// <param name="request">Request DTO instance</param>
        /// <returns>Deserialized reply DTO</returns>
        public static TReply Invoke<TRequest, TReply>(
            string url,
            string soapAction,
            string requestElementName,
            string replyElementName,
            string operationNamespace,
            TRequest request)
        {
            //--- serialize the request DTO as the bare body element ---
            var requestSerializer = new XmlSerializer(typeof(TRequest),
                new XmlRootAttribute(requestElementName) { Namespace = operationNamespace });

            var bodyXml = new StringBuilder();
            var writerSettings = new XmlWriterSettings { OmitXmlDeclaration = true, Encoding = new UTF8Encoding(false) };
            using (var sw = new StringWriter(bodyXml))
            using (var xw = XmlWriter.Create(sw, writerSettings))
            {
                //SoapHttpClientProtocol declared xsi/xsd on the serialized element; reproduce so
                //xsi:nil and any xsi:type behave identically on the wire.
                var ns = new XmlSerializerNamespaces();
                ns.Add("xsi", XsiNs);
                ns.Add("xsd", XsdNs);
                requestSerializer.Serialize(xw, request, ns);
            }

            //--- wrap in a SOAP 1.1 envelope ---
            var envelope = new StringBuilder();
            envelope.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            envelope.Append("<soap:Envelope xmlns:soap=\"").Append(SoapEnvelopeNs).Append("\">");
            envelope.Append("<soap:Body>");
            envelope.Append(bodyXml);
            envelope.Append("</soap:Body>");
            envelope.Append("</soap:Envelope>");

            //--- POST ---
            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, url))
            {
                var content = new StringContent(envelope.ToString(), new UTF8Encoding(false));
                //SOAP 1.1 content type
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };
                httpRequest.Content = content;
                //SOAP 1.1 requires a (quoted) SOAPAction HTTP header
                httpRequest.Headers.TryAddWithoutValidation("SOAPAction", "\"" + soapAction + "\"");

                HttpResponseMessage httpResponse;
                try
                {
                    httpResponse = _httpClient.Send(httpRequest, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    //network/transport failure - surface as a plain exception, as
                    //SoapHttpClientProtocol did (the call sites also catch (Exception)).
                    throw new Exception("FedEx SOAP request failed: " + ex.Message, ex);
                }

                string responseXml;
                using (var stream = httpResponse.Content.ReadAsStream())
                using (var reader = new StreamReader(stream))
                {
                    responseXml = reader.ReadToEnd();
                }

                //--- parse the envelope ---
                var doc = new XmlDocument();
                doc.LoadXml(responseXml);
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("soap", SoapEnvelopeNs);

                var body = doc.SelectSingleNode("/soap:Envelope/soap:Body", nsmgr);
                if (body == null)
                    throw new Exception("FedEx SOAP response had no Body element.");

                //--- SOAP fault? surface as SoapException with its <detail> node ---
                var fault = body.SelectSingleNode("soap:Fault", nsmgr);
                if (fault != null)
                {
                    //SOAP 1.1 fault children are unqualified: faultstring, detail
                    var faultString = fault.SelectSingleNode("faultstring");
                    var detail = fault.SelectSingleNode("detail");
                    throw new SoapException(faultString != null ? faultString.InnerText : "SOAP fault", detail);
                }

                //--- deserialize the bare reply element ---
                var replyNode = FindReplyElement(body, replyElementName, operationNamespace);
                if (replyNode == null)
                    throw new Exception(string.Format("FedEx SOAP response Body had no '{0}' element.", replyElementName));

                var replySerializer = new XmlSerializer(typeof(TReply),
                    new XmlRootAttribute(replyElementName) { Namespace = operationNamespace });
                using (var replyReader = new XmlNodeReader(replyNode))
                {
                    return (TReply)replySerializer.Deserialize(replyReader);
                }
            }
        }

        private static XmlNode FindReplyElement(XmlNode body, string localName, string ns)
        {
            foreach (XmlNode child in body.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element &&
                    string.Equals(child.LocalName, localName, StringComparison.Ordinal))
                {
                    return child;
                }
            }
            //fall back to first element child (some services omit an exact namespace match)
            foreach (XmlNode child in body.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                    return child;
            }
            return null;
        }
    }
}
