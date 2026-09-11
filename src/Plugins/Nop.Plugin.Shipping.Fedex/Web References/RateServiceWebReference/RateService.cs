namespace Nop.Plugin.Shipping.Fedex.RateServiceWebReference
{
    /// <summary>
    /// Hand-built replacement for the WSDL-generated <c>RateService</c> SOAP proxy (task 14.3).
    /// </summary>
    /// <remarks>
    /// The original derived from <c>System.Web.Services.Protocols.SoapHttpClientProtocol</c>,
    /// which has no net10.0 counterpart. Following the 4.2 precedent, only the proxy class was
    /// replaced; every DTO in <c>Reference.cs</c> is kept. This class preserves the exact public
    /// surface the plugin uses — a parameterless constructor, a settable <see cref="Url"/>, and
    /// <see cref="getRates"/> — and the exact SOAP 1.1 document/literal/bare wire shape the
    /// generated proxy sent, so the request FedEx receives is unchanged (see the method
    /// attributes that were on the generated code, transcribed into the constants below).
    ///
    /// The async members (<c>getRatesAsync</c> / <c>getRatesCompleted</c>) and the
    /// <c>UseDefaultCredentials</c> / local-file-system-webservice plumbing were dropped: nothing
    /// in the plugin called them, and the computation method always assigns <see cref="Url"/> to
    /// <c>_fedexSettings.Url</c> before calling, so the generated default-URL logic (which read
    /// <c>Properties.Settings</c>) was dead.
    /// </remarks>
    public partial class RateService
    {
        //Transcribed from the generated method's attributes:
        //  [SoapDocumentMethod("http://fedex.com/ws/rate/v16/getRates", Use=Literal, ParameterStyle=Bare)]
        //  [return: XmlElement("RateReply", Namespace="http://fedex.com/ws/rate/v16")]
        //  getRates([XmlElement(Namespace="http://fedex.com/ws/rate/v16")] RateRequest RateRequest)
        private const string OperationNamespace = "http://fedex.com/ws/rate/v16";
        private const string SoapAction = "http://fedex.com/ws/rate/v16/getRates";
        private const string RequestElementName = "RateRequest";
        private const string ReplyElementName = "RateReply";

        /// <summary>
        /// Service endpoint URL. The computation method sets this to <c>FedexSettings.Url</c>.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets rates from FedEx. Same signature and wire shape as the generated proxy's method.
        /// </summary>
        public RateReply getRates(RateRequest RateRequest)
        {
            return FedexSoapInvoker.Invoke<RateRequest, RateReply>(
                Url, SoapAction, RequestElementName, ReplyElementName, OperationNamespace, RateRequest);
        }
    }
}
