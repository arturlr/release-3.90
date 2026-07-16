using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Nop.Plugin.Shipping.Fedex.RateServiceWebReference
{
    public class WebAuthenticationDetail
    {
        public WebAuthenticationCredential UserCredential { get; set; }
    }
    public class WebAuthenticationCredential
    {
        public string Key { get; set; }
        public string Password { get; set; }
    }
    public class ClientDetail
    {
        public string AccountNumber { get; set; }
        public string MeterNumber { get; set; }
    }
    public class TransactionDetail
    {
        public string CustomerTransactionId { get; set; }
    }
    public class VersionId { }
    public class Address
    {
        public string[] StreetLines { get; set; }
        public string City { get; set; }
        public string StateOrProvinceCode { get; set; }
        public string PostalCode { get; set; }
        public string CountryCode { get; set; }
        public bool Residential { get; set; }
        public bool ResidentialSpecified { get; set; }
    }
    public class Party
    {
        public Address Address { get; set; }
        public string AccountNumber { get; set; }
    }
    public class Payment
    {
        public PaymentType PaymentType { get; set; }
        public bool PaymentTypeSpecified { get; set; }
        public Payor Payor { get; set; }
    }
    public class Payor
    {
        public Party ResponsibleParty { get; set; }
    }
    public class Money
    {
        public decimal Amount { get; set; }
        public bool AmountSpecified { get; set; }
        public string Currency { get; set; }
    }
    public class Weight
    {
        public WeightUnits Units { get; set; }
        public bool UnitsSpecified { get; set; }
        public decimal Value { get; set; }
        public bool ValueSpecified { get; set; }
    }
    public class Dimensions
    {
        public string Length { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public LinearUnits Units { get; set; }
        public bool UnitsSpecified { get; set; }
    }
    public class RequestedPackageLineItem
    {
        public string SequenceNumber { get; set; }
        public string GroupPackageCount { get; set; }
        public Weight Weight { get; set; }
        public Dimensions Dimensions { get; set; }
        public Money InsuredValue { get; set; }
    }
    public class RequestedShipment
    {
        public Party Shipper { get; set; }
        public Party Recipient { get; set; }
        public Payment ShippingChargesPayment { get; set; }
        public DropoffType DropoffType { get; set; }
        public Money TotalInsuredValue { get; set; }
        public DateTime ShipTimestamp { get; set; }
        public bool ShipTimestampSpecified { get; set; }
        public RateRequestType[] RateRequestTypes { get; set; }
        public string PackageCount { get; set; }
        public RequestedPackageLineItem[] RequestedPackageLineItems { get; set; }
        public CustomsClearanceDetail CustomsClearanceDetail { get; set; }
    }
    public class CustomsClearanceDetail
    {
        public CommercialInvoice CommercialInvoice { get; set; }
        public Commodity[] Commodities { get; set; }
    }
    public class CommercialInvoice
    {
        public PurposeOfShipmentType Purpose { get; set; }
        public bool PurposeSpecified { get; set; }
    }
    public class Commodity
    {
        public string Name { get; set; }
        public string NumberOfPieces { get; set; }
        public Money CustomsValue { get; set; }
    }
    public class RateRequest
    {
        public WebAuthenticationDetail WebAuthenticationDetail { get; set; }
        public ClientDetail ClientDetail { get; set; }
        public TransactionDetail TransactionDetail { get; set; }
        public VersionId Version { get; set; }
        public bool ReturnTransitAndCommit { get; set; }
        public bool ReturnTransitAndCommitSpecified { get; set; }
        public CarrierCodeType[] CarrierCodes { get; set; }
        public RequestedShipment RequestedShipment { get; set; }
    }
    public class RateReply
    {
        public NotificationSeverityType HighestSeverity { get; set; }
        public Notification[] Notifications { get; set; }
        public RateReplyDetail[] RateReplyDetails { get; set; }
    }
    public class Notification
    {
        public string Message { get; set; }
        public string Code { get; set; }
    }
    public class RateReplyDetail
    {
        public ServiceType ServiceType { get; set; }
        public RatedShipmentDetail[] RatedShipmentDetails { get; set; }
    }
    public class RatedShipmentDetail
    {
        public ShipmentRateDetail ShipmentRateDetail { get; set; }
    }
    public class ShipmentRateDetail
    {
        public ReturnedRateType RateType { get; set; }
        public Weight TotalBillingWeight { get; set; }
        public Money TotalBaseCharge { get; set; }
        public Money TotalFreightDiscounts { get; set; }
        public Money TotalSurcharges { get; set; }
        public Money TotalNetCharge { get; set; }
    }
    public enum CarrierCodeType { FDXE, FDXG }
    public enum DropoffType { BUSINESS_SERVICE_CENTER, DROP_BOX, REGULAR_PICKUP, REQUEST_COURIER, STATION }
    public enum LinearUnits { IN, CM }
    public enum WeightUnits { LB, KG }
    public enum NotificationSeverityType { SUCCESS, NOTE, WARNING, ERROR, FAILURE }
    public enum PaymentType { SENDER, RECIPIENT, THIRD_PARTY }
    public enum PurposeOfShipmentType { SOLD, GIFT, SAMPLE, RETURN, REPAIR }
    public enum RateRequestType { PREFERRED, LIST }
    public enum ReturnedRateType { PAYOR_ACCOUNT_PACKAGE, PAYOR_ACCOUNT_SHIPMENT, PAYOR_LIST_PACKAGE, PAYOR_LIST_SHIPMENT, RATED_ACCOUNT, PAYOR_MULTIWEIGHT, RATED_LIST }
    public enum ServiceType
    {
        FEDEX_GROUND,
        FEDEX_2_DAY,
        FEDEX_2_DAY_AM,
        FEDEX_EXPRESS_SAVER,
        FIRST_OVERNIGHT,
        GROUND_HOME_DELIVERY,
        INTERNATIONAL_ECONOMY,
        INTERNATIONAL_FIRST,
        INTERNATIONAL_PRIORITY,
        PRIORITY_OVERNIGHT,
        SMART_POST,
        STANDARD_OVERNIGHT,
        FEDEX_1_DAY_FREIGHT,
        FEDEX_2_DAY_FREIGHT,
        FEDEX_3_DAY_FREIGHT,
        INTERNATIONAL_ECONOMY_FREIGHT,
        INTERNATIONAL_PRIORITY_FREIGHT,
        FEDEX_FREIGHT_ECONOMY,
        FEDEX_FREIGHT_PRIORITY,
        FEDEX_FIRST_FREIGHT,
        EUROPE_FIRST_INTERNATIONAL_PRIORITY
    }

    /// <summary>
    /// HttpClient-based rate service for FedEx SOAP API
    /// </summary>
    public class RateService
    {
        private readonly HttpClient _httpClient;
        public string Url { get; set; }

        public RateService()
        {
            _httpClient = new HttpClient();
        }

        public RateReply getRates(RateRequest request)
        {
            // Build SOAP envelope
            var soapEnvelope = BuildRateSoapEnvelope(request);
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "getRates");

            var response = _httpClient.PostAsync(Url, content).GetAwaiter().GetResult();
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                return new RateReply
                {
                    HighestSeverity = NotificationSeverityType.ERROR,
                    Notifications = new[] { new Notification { Message = "Service unavailable: " + response.StatusCode, Code = ((int)response.StatusCode).ToString() } }
                };
            }

            return ParseRateReply(responseContent);
        }

        private string BuildRateSoapEnvelope(RateRequest request)
        {
            var sb = new StringBuilder();
            sb.Append(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v16=""http://fedex.com/ws/rate/v16"">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<v16:RateRequest>");
            sb.AppendFormat("<v16:WebAuthenticationDetail><v16:UserCredential><v16:Key>{0}</v16:Key><v16:Password>{1}</v16:Password></v16:UserCredential></v16:WebAuthenticationDetail>",
                Escape(request.WebAuthenticationDetail?.UserCredential?.Key),
                Escape(request.WebAuthenticationDetail?.UserCredential?.Password));
            sb.AppendFormat("<v16:ClientDetail><v16:AccountNumber>{0}</v16:AccountNumber><v16:MeterNumber>{1}</v16:MeterNumber></v16:ClientDetail>",
                Escape(request.ClientDetail?.AccountNumber),
                Escape(request.ClientDetail?.MeterNumber));
            sb.Append("<v16:Version><v16:ServiceId>crs</v16:ServiceId><v16:Major>16</v16:Major><v16:Intermediate>0</v16:Intermediate><v16:Minor>0</v16:Minor></v16:Version>");
            sb.Append("<v16:ReturnTransitAndCommit>true</v16:ReturnTransitAndCommit>");
            if (request.RequestedShipment != null)
            {
                sb.Append("<v16:RequestedShipment>");
                if (request.RequestedShipment.ShipTimestampSpecified)
                    sb.AppendFormat("<v16:ShipTimestamp>{0}</v16:ShipTimestamp>", request.RequestedShipment.ShipTimestamp.ToString("o"));
                sb.AppendFormat("<v16:DropoffType>{0}</v16:DropoffType>", request.RequestedShipment.DropoffType);
                if (request.RequestedShipment.Shipper != null)
                    sb.AppendFormat("<v16:Shipper><v16:Address><v16:PostalCode>{0}</v16:PostalCode><v16:CountryCode>{1}</v16:CountryCode></v16:Address></v16:Shipper>",
                        Escape(request.RequestedShipment.Shipper.Address?.PostalCode),
                        Escape(request.RequestedShipment.Shipper.Address?.CountryCode));
                if (request.RequestedShipment.Recipient != null)
                    sb.AppendFormat("<v16:Recipient><v16:Address><v16:PostalCode>{0}</v16:PostalCode><v16:CountryCode>{1}</v16:CountryCode>{2}</v16:Address></v16:Recipient>",
                        Escape(request.RequestedShipment.Recipient.Address?.PostalCode),
                        Escape(request.RequestedShipment.Recipient.Address?.CountryCode),
                        request.RequestedShipment.Recipient.Address?.Residential == true ? "<v16:Residential>true</v16:Residential>" : "");
                sb.AppendFormat("<v16:PackageCount>{0}</v16:PackageCount>", request.RequestedShipment.PackageCount ?? "1");
                if (request.RequestedShipment.RequestedPackageLineItems != null)
                {
                    foreach (var pkg in request.RequestedShipment.RequestedPackageLineItems)
                    {
                        if (pkg == null) continue;
                        sb.Append("<v16:RequestedPackageLineItems>");
                        sb.AppendFormat("<v16:SequenceNumber>{0}</v16:SequenceNumber>", pkg.SequenceNumber);
                        sb.AppendFormat("<v16:GroupPackageCount>{0}</v16:GroupPackageCount>", pkg.GroupPackageCount);
                        if (pkg.Weight != null)
                            sb.AppendFormat("<v16:Weight><v16:Units>{0}</v16:Units><v16:Value>{1}</v16:Value></v16:Weight>", pkg.Weight.Units, pkg.Weight.Value);
                        if (pkg.Dimensions != null)
                            sb.AppendFormat("<v16:Dimensions><v16:Length>{0}</v16:Length><v16:Width>{1}</v16:Width><v16:Height>{2}</v16:Height><v16:Units>{3}</v16:Units></v16:Dimensions>",
                                pkg.Dimensions.Length, pkg.Dimensions.Width, pkg.Dimensions.Height, pkg.Dimensions.Units);
                        sb.Append("</v16:RequestedPackageLineItems>");
                    }
                }
                sb.Append("</v16:RequestedShipment>");
            }
            sb.Append("</v16:RateRequest>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");
            return sb.ToString();
        }

        private RateReply ParseRateReply(string xml)
        {
            var reply = new RateReply();
            try
            {
                var doc = System.Xml.Linq.XDocument.Parse(xml);
                var ns = "http://fedex.com/ws/rate/v16";
                var body = doc.Descendants(System.Xml.Linq.XName.Get("RateReply", ns)).FirstOrDefault();
                if (body == null)
                {
                    reply.HighestSeverity = NotificationSeverityType.ERROR;
                    reply.Notifications = new[] { new Notification { Message = "Invalid response from FedEx", Code = "0" } };
                    return reply;
                }

                var severity = body.Element(System.Xml.Linq.XName.Get("HighestSeverity", ns))?.Value;
                if (Enum.TryParse<NotificationSeverityType>(severity, true, out var sev))
                    reply.HighestSeverity = sev;

                var notifications = body.Elements(System.Xml.Linq.XName.Get("Notifications", ns));
                var notifList = new System.Collections.Generic.List<Notification>();
                foreach (var n in notifications)
                {
                    notifList.Add(new Notification
                    {
                        Message = n.Element(System.Xml.Linq.XName.Get("Message", ns))?.Value,
                        Code = n.Element(System.Xml.Linq.XName.Get("Code", ns))?.Value
                    });
                }
                reply.Notifications = notifList.ToArray();

                var details = body.Elements(System.Xml.Linq.XName.Get("RateReplyDetails", ns));
                var detailList = new System.Collections.Generic.List<RateReplyDetail>();
                foreach (var d in details)
                {
                    var detail = new RateReplyDetail();
                    var serviceTypeStr = d.Element(System.Xml.Linq.XName.Get("ServiceType", ns))?.Value;
                    if (Enum.TryParse<ServiceType>(serviceTypeStr, true, out var st))
                        detail.ServiceType = st;

                    var shipmentDetails = d.Elements(System.Xml.Linq.XName.Get("RatedShipmentDetails", ns));
                    var rsdList = new System.Collections.Generic.List<RatedShipmentDetail>();
                    foreach (var sd in shipmentDetails)
                    {
                        var rsd = new RatedShipmentDetail();
                        var srdElem = sd.Element(System.Xml.Linq.XName.Get("ShipmentRateDetail", ns));
                        if (srdElem != null)
                        {
                            rsd.ShipmentRateDetail = new ShipmentRateDetail();
                            var rateTypeStr = srdElem.Element(System.Xml.Linq.XName.Get("RateType", ns))?.Value;
                            if (Enum.TryParse<ReturnedRateType>(rateTypeStr, true, out var rt))
                                rsd.ShipmentRateDetail.RateType = rt;
                            rsd.ShipmentRateDetail.TotalBillingWeight = ParseWeight(srdElem.Element(System.Xml.Linq.XName.Get("TotalBillingWeight", ns)), ns);
                            rsd.ShipmentRateDetail.TotalBaseCharge = ParseMoney(srdElem.Element(System.Xml.Linq.XName.Get("TotalBaseCharge", ns)), ns);
                            rsd.ShipmentRateDetail.TotalFreightDiscounts = ParseMoney(srdElem.Element(System.Xml.Linq.XName.Get("TotalFreightDiscounts", ns)), ns);
                            rsd.ShipmentRateDetail.TotalSurcharges = ParseMoney(srdElem.Element(System.Xml.Linq.XName.Get("TotalSurcharges", ns)), ns);
                            rsd.ShipmentRateDetail.TotalNetCharge = ParseMoney(srdElem.Element(System.Xml.Linq.XName.Get("TotalNetCharge", ns)), ns);
                        }
                        rsdList.Add(rsd);
                    }
                    detail.RatedShipmentDetails = rsdList.ToArray();
                    detailList.Add(detail);
                }
                reply.RateReplyDetails = detailList.ToArray();
            }
            catch (Exception ex)
            {
                reply.HighestSeverity = NotificationSeverityType.ERROR;
                reply.Notifications = new[] { new Notification { Message = "Error parsing FedEx response: " + ex.Message, Code = "0" } };
            }
            return reply;
        }

        private Money ParseMoney(System.Xml.Linq.XElement elem, string ns)
        {
            if (elem == null) return new Money();
            decimal.TryParse(elem.Element(System.Xml.Linq.XName.Get("Amount", ns))?.Value, out var amount);
            return new Money { Amount = amount, Currency = elem.Element(System.Xml.Linq.XName.Get("Currency", ns))?.Value };
        }

        private Weight ParseWeight(System.Xml.Linq.XElement elem, string ns)
        {
            if (elem == null) return new Weight();
            decimal.TryParse(elem.Element(System.Xml.Linq.XName.Get("Value", ns))?.Value, out var val);
            return new Weight { Value = val };
        }

        private static System.Xml.Linq.XElement FirstOrDefault(System.Collections.Generic.IEnumerable<System.Xml.Linq.XElement> elements)
        {
            foreach (var e in elements) return e;
            return null;
        }

        private string Escape(string value)
        {
            return System.Security.SecurityElement.Escape(value ?? string.Empty);
        }
    }
}
