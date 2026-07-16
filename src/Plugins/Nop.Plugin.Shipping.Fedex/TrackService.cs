using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Nop.Plugin.Shipping.Fedex
{
    // Track service types used by FedexShipmentTracker
    public class TrackRequest
    {
        public RateServiceWebReference.WebAuthenticationDetail WebAuthenticationDetail { get; set; }
        public RateServiceWebReference.ClientDetail ClientDetail { get; set; }
        public RateServiceWebReference.TransactionDetail TransactionDetail { get; set; }
        public RateServiceWebReference.VersionId Version { get; set; }
        public TrackPackageIdentifier PackageIdentifier { get; set; }
        public bool IncludeDetailedScans { get; set; }
        public bool IncludeDetailedScansSpecified { get; set; }
    }

    public class TrackPackageIdentifier
    {
        public string Value { get; set; }
        public TrackIdentifierType Type { get; set; }
    }

    public enum TrackIdentifierType
    {
        TRACKING_NUMBER_OR_DOORTAG
    }

    public class TrackReply
    {
        public RateServiceWebReference.NotificationSeverityType HighestSeverity { get; set; }
        public TrackDetail[] TrackDetails { get; set; }
    }

    public class TrackDetail
    {
        public TrackEvent[] Events { get; set; }
    }

    public class TrackEvent
    {
        public DateTime Timestamp { get; set; }
        public bool TimestampSpecified { get; set; }
        public string EventDescription { get; set; }
        public string EventType { get; set; }
        public TrackAddress Address { get; set; }
    }

    public class TrackAddress
    {
        public string City { get; set; }
        public string StateOrProvinceCode { get; set; }
        public string CountryCode { get; set; }
        public string PostalCode { get; set; }
    }

    /// <summary>
    /// HttpClient-based track service for FedEx
    /// </summary>
    public class TrackService
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;

        public TrackService(string url)
        {
            _url = url;
            _httpClient = new HttpClient();
        }

        public TrackReply track(TrackRequest request)
        {
            var soapEnvelope = BuildTrackSoapEnvelope(request);
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "track");

            var response = _httpClient.PostAsync(_url, content).GetAwaiter().GetResult();
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                return new TrackReply
                {
                    HighestSeverity = RateServiceWebReference.NotificationSeverityType.ERROR,
                    TrackDetails = new TrackDetail[0]
                };
            }

            return ParseTrackReply(responseContent);
        }

        private string BuildTrackSoapEnvelope(TrackRequest request)
        {
            var sb = new StringBuilder();
            sb.Append(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v9=""http://fedex.com/ws/track/v9"">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<v9:TrackRequest>");
            sb.AppendFormat("<v9:WebAuthenticationDetail><v9:UserCredential><v9:Key>{0}</v9:Key><v9:Password>{1}</v9:Password></v9:UserCredential></v9:WebAuthenticationDetail>",
                Escape(request.WebAuthenticationDetail?.UserCredential?.Key),
                Escape(request.WebAuthenticationDetail?.UserCredential?.Password));
            sb.AppendFormat("<v9:ClientDetail><v9:AccountNumber>{0}</v9:AccountNumber><v9:MeterNumber>{1}</v9:MeterNumber></v9:ClientDetail>",
                Escape(request.ClientDetail?.AccountNumber),
                Escape(request.ClientDetail?.MeterNumber));
            sb.Append("<v9:Version><v9:ServiceId>trck</v9:ServiceId><v9:Major>9</v9:Major><v9:Intermediate>1</v9:Intermediate><v9:Minor>0</v9:Minor></v9:Version>");
            sb.AppendFormat("<v9:PackageIdentifier><v9:Value>{0}</v9:Value><v9:Type>TRACKING_NUMBER_OR_DOORTAG</v9:Type></v9:PackageIdentifier>",
                Escape(request.PackageIdentifier?.Value));
            sb.Append("<v9:IncludeDetailedScans>true</v9:IncludeDetailedScans>");
            sb.Append("</v9:TrackRequest>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");
            return sb.ToString();
        }

        private TrackReply ParseTrackReply(string xml)
        {
            var reply = new TrackReply();
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xml);
                var ns = "http://fedex.com/ws/track/v9";
                var body = doc.Descendants(System.Xml.Linq.XName.Get("TrackReply", ns)).FirstOrDefault();
                if (body == null)
                {
                    reply.HighestSeverity = RateServiceWebReference.NotificationSeverityType.ERROR;
                    return reply;
                }

                var severity = body.Element(System.Xml.Linq.XName.Get("HighestSeverity", ns))?.Value;
                if (Enum.TryParse<RateServiceWebReference.NotificationSeverityType>(severity, true, out var sev))
                    reply.HighestSeverity = sev;

                var trackDetails = body.Elements(System.Xml.Linq.XName.Get("TrackDetails", ns));
                var detailList = new List<TrackDetail>();
                foreach (var td in trackDetails)
                {
                    var detail = new TrackDetail();
                    var events = td.Elements(System.Xml.Linq.XName.Get("Events", ns));
                    var eventList = new List<TrackEvent>();
                    foreach (var e in events)
                    {
                        var evt = new TrackEvent();
                        var tsStr = e.Element(System.Xml.Linq.XName.Get("Timestamp", ns))?.Value;
                        if (DateTime.TryParse(tsStr, out var ts))
                        {
                            evt.Timestamp = ts;
                            evt.TimestampSpecified = true;
                        }
                        evt.EventDescription = e.Element(System.Xml.Linq.XName.Get("EventDescription", ns))?.Value;
                        evt.EventType = e.Element(System.Xml.Linq.XName.Get("EventType", ns))?.Value;
                        var addr = e.Element(System.Xml.Linq.XName.Get("Address", ns));
                        evt.Address = new TrackAddress
                        {
                            City = addr?.Element(System.Xml.Linq.XName.Get("City", ns))?.Value,
                            StateOrProvinceCode = addr?.Element(System.Xml.Linq.XName.Get("StateOrProvinceCode", ns))?.Value,
                            CountryCode = addr?.Element(System.Xml.Linq.XName.Get("CountryCode", ns))?.Value,
                            PostalCode = addr?.Element(System.Xml.Linq.XName.Get("PostalCode", ns))?.Value
                        };
                        eventList.Add(evt);
                    }
                    detail.Events = eventList.ToArray();
                    detailList.Add(detail);
                }
                reply.TrackDetails = detailList.ToArray();
            }
            catch
            {
                reply.HighestSeverity = RateServiceWebReference.NotificationSeverityType.ERROR;
                reply.TrackDetails = new TrackDetail[0];
            }
            return reply;
        }

        private string Escape(string value)
        {
            return System.Security.SecurityElement.Escape(value ?? string.Empty);
        }
    }
}
