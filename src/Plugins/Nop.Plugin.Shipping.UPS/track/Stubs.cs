// Stub types for UPS SOAP Track Web Service - migration pending to UPS REST API
using System;
using System.Xml;

namespace Nop.Plugin.Shipping.UPS.track
{
    public class TrackService
    {
        public UPSSecurity UPSSecurityValue { get; set; }
        public string Url { get; set; }
        public TrackResponse ProcessTrack(TrackRequest request) 
        { 
            return new TrackResponse { Shipment = new ShipmentType[0] }; 
        }
    }

    public class TrackRequest
    {
        public RequestType Request { get; set; }
        public string InquiryNumber { get; set; }
    }

    public class RequestType
    {
        public string[] RequestOption { get; set; }
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

    public class SoapException : Exception
    {
        public XmlElement Detail { get; set; }
        public SoapException() { }
        public SoapException(string message) : base(message) { }
    }
}
