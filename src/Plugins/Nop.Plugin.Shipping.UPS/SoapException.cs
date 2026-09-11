using System;
using System.Xml;

namespace Nop.Plugin.Shipping.UPS
{
    /// <summary>
    /// Lightweight replacement for <c>System.Web.Services.Protocols.SoapException</c>, which has
    /// no net10.0 counterpart. Task 14.5 — following the 4.2 precedent and the 14.3 FedEx port.
    ///
    /// <c>UPSShipmentTracker</c>'s catch site reads <c>ex.Detail.LastChild.InnerText</c>, so the
    /// replacement exposes an <see cref="XmlNode"/> <see cref="Detail"/> with the same shape: the
    /// &lt;detail&gt; element of the SOAP 1.1 fault, parsed from the carrier's actual XML.
    /// </summary>
    public class SoapException : Exception
    {
        public SoapException(string message, XmlNode detail)
            : base(message)
        {
            Detail = detail;
        }

        public SoapException(string message, XmlNode detail, Exception innerException)
            : base(message, innerException)
        {
            Detail = detail;
        }

        /// <summary>
        /// The &lt;detail&gt; element of the SOAP 1.1 fault, as an <see cref="XmlNode"/>.
        /// Matches <c>System.Web.Services.Protocols.SoapException.Detail</c>'s type and shape.
        /// </summary>
        public XmlNode Detail { get; }
    }
}
