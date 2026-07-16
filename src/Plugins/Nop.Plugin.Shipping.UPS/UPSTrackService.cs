using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Nop.Plugin.Shipping.UPS.track
{
    public class UPSSecurity
    {
        public UPSSecurityServiceAccessToken ServiceAccessToken { get; set; }
        public UPSSecurityUsernameToken UsernameToken { get; set; }
    }

    public class UPSSecurityServiceAccessToken
    {
        public string AccessLicenseNumber { get; set; }
    }

    public class UPSSecurityUsernameToken
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RequestType
    {
        public string[] RequestOption { get; set; }
    }

    public class TrackRequest
    {
        public RequestType Request { get; set; }
        public string InquiryNumber { get; set; }
    }

    public class TrackResponse
    {
        public ShipmentType[] Shipment { get; set; }
    }

    public class ShipmentType
    {
        public PackageType[] Package { get; set; }
    }

    public class PackageType
    {
        public ActivityType[] Activity { get; set; }
    }

    public class ActivityType
    {
        public StatusType Status { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public ActivityLocationType ActivityLocation { get; set; }
    }

    public class StatusType
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }

    public class ActivityLocationType
    {
        public AddressType Address { get; set; }
    }

    public class AddressType
    {
        public string City { get; set; }
        public string StateProvinceCode { get; set; }
        public string CountryCode { get; set; }
    }

    /// <summary>
    /// HttpClient-based UPS Track service
    /// </summary>
    public class TrackService
    {
        private readonly HttpClient _httpClient;
        public UPSSecurity UPSSecurityValue { get; set; }

        private const string TrackUrl = "https://onlinetools.ups.com/ups.app/xml/Track";

        public TrackService()
        {
            _httpClient = new HttpClient();
        }

        public TrackResponse ProcessTrack(TrackRequest request)
        {
            var accessXml = string.Format(
                "<?xml version=\"1.0\"?><AccessRequest xml:lang=\"en-US\"><AccessLicenseNumber>{0}</AccessLicenseNumber><UserId>{1}</UserId><Password>{2}</Password></AccessRequest>",
                Escape(UPSSecurityValue?.ServiceAccessToken?.AccessLicenseNumber),
                Escape(UPSSecurityValue?.UsernameToken?.Username),
                Escape(UPSSecurityValue?.UsernameToken?.Password));

            var trackXml = string.Format(
                "<?xml version=\"1.0\"?><TrackRequest xml:lang=\"en-US\"><Request><TransactionReference><CustomerContext>Track</CustomerContext></TransactionReference><RequestAction>Track</RequestAction><RequestOption>{0}</RequestOption></Request><TrackingNumber>{1}</TrackingNumber></TrackRequest>",
                request.Request?.RequestOption != null && request.Request.RequestOption.Length > 0 ? Escape(request.Request.RequestOption[0]) : "15",
                Escape(request.InquiryNumber));

            var fullRequest = accessXml + trackXml;
            var content = new StringContent(fullRequest, Encoding.UTF8, "application/xml");

            var response = _httpClient.PostAsync(TrackUrl, content).GetAwaiter().GetResult();
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            return ParseTrackResponse(responseContent);
        }

        private TrackResponse ParseTrackResponse(string xml)
        {
            var trackResponse = new TrackResponse();
            try
            {
                var doc = XDocument.Parse(xml);
                var shipmentElements = doc.Descendants("Shipment");
                var shipments = new List<ShipmentType>();

                foreach (var shipElem in shipmentElements)
                {
                    var shipment = new ShipmentType();
                    var packageElements = shipElem.Elements("Package");
                    var packages = new List<PackageType>();

                    foreach (var pkgElem in packageElements)
                    {
                        var package = new PackageType();
                        var activityElements = pkgElem.Elements("Activity");
                        var activities = new List<ActivityType>();

                        foreach (var actElem in activityElements)
                        {
                            var activity = new ActivityType();
                            var statusElem = actElem.Element("Status");
                            if (statusElem != null)
                            {
                                var statusTypeElem = statusElem.Element("StatusType");
                                activity.Status = new StatusType
                                {
                                    Code = statusTypeElem?.Element("Code")?.Value,
                                    Type = statusTypeElem?.Element("Code")?.Value,
                                    Description = statusTypeElem?.Element("Description")?.Value
                                };
                                var statusCodeElem = statusElem.Element("StatusCode");
                                if (statusCodeElem != null)
                                    activity.Status.Code = statusCodeElem.Element("Code")?.Value;
                            }
                            activity.Date = actElem.Element("Date")?.Value ?? "";
                            activity.Time = actElem.Element("Time")?.Value ?? "";
                            var locElem = actElem.Element("ActivityLocation");
                            activity.ActivityLocation = new ActivityLocationType
                            {
                                Address = new AddressType
                                {
                                    City = locElem?.Element("Address")?.Element("City")?.Value,
                                    StateProvinceCode = locElem?.Element("Address")?.Element("StateProvinceCode")?.Value,
                                    CountryCode = locElem?.Element("Address")?.Element("CountryCode")?.Value
                                }
                            };
                            activities.Add(activity);
                        }
                        package.Activity = activities.ToArray();
                        packages.Add(package);
                    }
                    shipment.Package = packages.ToArray();
                    shipments.Add(shipment);
                }
                trackResponse.Shipment = shipments.ToArray();
            }
            catch
            {
                trackResponse.Shipment = new ShipmentType[0];
            }
            return trackResponse;
        }

        private static string Escape(string value)
        {
            return System.Security.SecurityElement.Escape(value ?? string.Empty);
        }
    }
}
