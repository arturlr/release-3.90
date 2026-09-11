namespace Nop.Plugin.Shipping.UPS.track
{
    /// <summary>
    /// Hand-built replacement for the WSDL-generated <c>TrackService</c> SOAP proxy (task 14.5).
    /// </summary>
    /// <remarks>
    /// The original derived from <c>System.Web.Services.Protocols.SoapHttpClientProtocol</c>, which
    /// has no net10.0 counterpart. Following the 4.2 precedent and the 14.3 FedEx port, only the
    /// proxy class was replaced; every DTO in <c>Reference.cs</c> is kept. This class preserves the
    /// exact public surface the tracker uses — a parameterless constructor, a settable
    /// <see cref="UPSSecurityValue"/> and <see cref="ProcessTrack"/> — and the exact SOAP 1.1
    /// document/literal/bare wire shape, INCLUDING the <c>UPSSecurity</c> SOAP header the generated
    /// proxy carried via <c>[SoapHeader("UPSSecurityValue")]</c>.
    ///
    /// The async members and the <c>UseDefaultCredentials</c> / default-URL plumbing (which read
    /// <c>Properties.Settings</c>) were dropped: nothing in the plugin used them. The tracker sets
    /// <see cref="Url"/> explicitly is NOT done in 3.90 — the proxy defaulted the URL from
    /// <c>Properties.Settings</c>; that default is transcribed into <see cref="DefaultUrl"/> below so
    /// behaviour is unchanged when the caller does not set <see cref="Url"/>.
    /// </remarks>
    public partial class TrackService
    {
        //Transcribed from the generated method's attributes:
        //  [SoapHeader("UPSSecurityValue")]
        //  [SoapDocumentMethod("http://onlinetools.ups.com/webservices/TrackBinding/v2.0", Use=Literal, ParameterStyle=Bare)]
        //  [return: XmlElement("TrackResponse", Namespace="http://www.ups.com/XMLSchema/XOLTWS/Track/v2.0")]
        //  ProcessTrack([XmlElement(Namespace="http://www.ups.com/XMLSchema/XOLTWS/Track/v2.0")] TrackRequest TrackRequest)
        private const string SoapAction = "http://onlinetools.ups.com/webservices/TrackBinding/v2.0";
        private const string OperationNamespace = "http://www.ups.com/XMLSchema/XOLTWS/Track/v2.0";
        private const string RequestElementName = "TrackRequest";
        private const string ReplyElementName = "TrackResponse";
        private const string HeaderElementName = "UPSSecurity";
        private const string HeaderNamespace = "http://www.ups.com/XMLSchema/XOLTWS/UPSS/v1.0";

        //The URL the generated proxy defaulted from Properties.Settings
        //(Nop_Plugin_Shipping_UPS_track_TrackService), transcribed verbatim so behaviour is
        //unchanged. NOTE this is UPS's CIE (test) endpoint, exactly as 3.90 shipped it.
        private const string DefaultUrl = "https://wwwcie.ups.com/webservices/Track";

        /// <summary>
        /// The UPSSecurity SOAP header value. Serialized into &lt;soap:Header&gt; by the invoker.
        /// </summary>
        public UPSSecurity UPSSecurityValue { get; set; }

        /// <summary>
        /// Service endpoint URL. Defaults to the value the generated proxy read from settings.
        /// </summary>
        public string Url { get; set; } = DefaultUrl;

        /// <remarks/>
        public TrackResponse ProcessTrack(TrackRequest TrackRequest)
        {
            return Nop.Plugin.Shipping.UPS.UpsSoapInvoker.Invoke<UPSSecurity, TrackRequest, TrackResponse>(
                Url, SoapAction,
                UPSSecurityValue, HeaderElementName, HeaderNamespace,
                RequestElementName, ReplyElementName, OperationNamespace,
                TrackRequest);
        }
    }
}
