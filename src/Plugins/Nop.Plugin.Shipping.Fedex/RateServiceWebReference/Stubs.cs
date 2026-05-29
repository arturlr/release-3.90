// Stub types for Fedex SOAP Web Service - migration pending to Fedex REST API
using System;

namespace Nop.Plugin.Shipping.Fedex.RateServiceWebReference
{
    public class RateService : IDisposable
    {
        public string Url { get; set; }
        public RateReply getRates(RateRequest request) { return new RateReply { HighestSeverity = NotificationSeverityType.SUCCESS, RateReplyDetails = new RatedShipmentDetail[0] }; }
        public void Dispose() { }
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
        public RatedShipmentDetail[] RateReplyDetails { get; set; }
    }

    public class Notification
    {
        public string Message { get; set; }
        public string Code { get; set; }
        public NotificationSeverityType Severity { get; set; }
    }

    public class RatedShipmentDetail
    {
        public ServiceType ServiceType { get; set; }
        public RatedShipmentDetail[] RatedShipmentDetails { get; set; }
        public ShipmentRateDetail ShipmentRateDetail { get; set; }
    }

    public class ShipmentRateDetail
    {
        public RatedRateType RateType { get; set; }
        public Money TotalNetCharge { get; set; }
        public Money TotalNetFreight { get; set; }
        public Money TotalSurcharges { get; set; }
        public Money TotalBaseCharge { get; set; }
        public Money TotalFreightDiscounts { get; set; }
        public string RatedWeightMethod { get; set; }
        public Weight TotalBillingWeight { get; set; }
    }

    public class Money
    {
        public string Currency { get; set; }
        public decimal Amount { get; set; }
        public bool AmountSpecified { get; set; }
    }

    public class RequestedShipment
    {
        public Party Shipper { get; set; }
        public Party Recipient { get; set; }
        public DropoffType DropoffType { get; set; }
        public bool DropoffTypeSpecified { get; set; }
        public ServiceType ServiceType { get; set; }
        public bool ServiceTypeSpecified { get; set; }
        public PackagingType PackagingType { get; set; }
        public bool PackagingTypeSpecified { get; set; }
        public Weight TotalWeight { get; set; }
        public Payment ShippingChargesPayment { get; set; }
        public string PackageCount { get; set; }
        public string PackageCountSpecified { get; set; }
        public RequestedPackageLineItem[] RequestedPackageLineItems { get; set; }
        public RateRequestType[] RateRequestTypes { get; set; }
        public Money TotalInsuredValue { get; set; }
        public bool PaymentTypeSpecified { get; set; }
        public DateTime ShipTimestamp { get; set; }
        public bool ShipTimestampSpecified { get; set; }
        public CustomsClearanceDetail CustomsClearanceDetail { get; set; }
        public bool ReturnTransitAndCommitSpecified { get; set; }
        public SmartPostShipmentDetail SmartPostDetail { get; set; }
        public bool SmartPostDetailSpecified { get; set; }
    }

    public class SmartPostShipmentDetail
    {
        public SmartPostIndiciaType Indicia { get; set; }
        public string HubId { get; set; }
    }

    public enum SmartPostIndiciaType { PARCEL_SELECT, MEDIA_MAIL, PRESORTED_STANDARD, PRESORTED_BOUND_PRINTED_MATTER }

    public class CustomsClearanceDetail
    {
        public Money CustomsValue { get; set; }
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

    public enum PurposeOfShipmentType { SOLD, GIFT, SAMPLE, RETURN, REPAIR }

    public class Party
    {
        public Address Address { get; set; }
        public Contact Contact { get; set; }
        public string AccountNumber { get; set; }
    }

    public class RequestedPackageLineItem
    {
        public string SequenceNumber { get; set; }
        public bool SequenceNumberSpecified { get; set; }
        public string GroupPackageCount { get; set; }
        public Weight Weight { get; set; }
        public Dimensions Dimensions { get; set; }
        public PackageSpecialServicesRequested SpecialServicesRequested { get; set; }
        public Money InsuredValue { get; set; }
    }

    public class PackageSpecialServicesRequested
    {
        public CodDetail CodDetail { get; set; }
    }

    public class CodDetail
    {
        public CodCollectionType CollectionType { get; set; }
        public Money CodCollectionAmount { get; set; }
    }

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

    public class Weight
    {
        public WeightUnits Units { get; set; }
        public bool UnitsSpecified { get; set; }
        public decimal Value { get; set; }
        public bool ValueSpecified { get; set; }
    }

    public class Dimensions
    {
        public LinearUnits Units { get; set; }
        public string Length { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public bool UnitsSpecified { get; set; }
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

    public class Contact
    {
        public string PersonName { get; set; }
        public string CompanyName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class WebAuthenticationDetail
    {
        public WebAuthenticationCredential UserCredential { get; set; }
        public WebAuthenticationCredential ParentCredential { get; set; }
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

    public class VersionId
    {
        public string ServiceId { get; set; }
        public int Major { get; set; }
        public int Intermediate { get; set; }
        public int Minor { get; set; }
    }

    public enum NotificationSeverityType { SUCCESS, NOTE, WARNING, ERROR, FAILURE }
    public enum DropoffType { BUSINESS_SERVICE_CENTER, DROP_BOX, REGULAR_PICKUP, REQUEST_COURIER, STATION }
    public enum ServiceType { FEDEX_GROUND, FEDEX_2_DAY, FEDEX_2_DAY_AM, FEDEX_EXPRESS_SAVER, STANDARD_OVERNIGHT, PRIORITY_OVERNIGHT, FIRST_OVERNIGHT, INTERNATIONAL_ECONOMY, INTERNATIONAL_PRIORITY, INTERNATIONAL_FIRST, GROUND_HOME_DELIVERY, SMART_POST, FEDEX_1_DAY_FREIGHT, FEDEX_2_DAY_FREIGHT, FEDEX_3_DAY_FREIGHT, INTERNATIONAL_ECONOMY_FREIGHT, INTERNATIONAL_PRIORITY_FREIGHT, FEDEX_FREIGHT_ECONOMY, FEDEX_FREIGHT_PRIORITY, EUROPE_FIRST_INTERNATIONAL_PRIORITY }
    public enum PackagingType { FEDEX_10KG_BOX, FEDEX_25KG_BOX, FEDEX_BOX, FEDEX_ENVELOPE, FEDEX_PAK, FEDEX_TUBE, YOUR_PACKAGING }
    public enum PaymentType { SENDER, THIRD_PARTY, COLLECT, RECIPIENT }
    public enum WeightUnits { LB, KG }
    public enum LinearUnits { IN, CM }
    public enum CarrierCodeType { FDXE, FDXG }
    public enum RateRequestType { LIST, NONE, ACCOUNT, PREFERRED }
    public enum RatedRateType { PAYOR_ACCOUNT_SHIPMENT, PAYOR_ACCOUNT_PACKAGE, PAYOR_LIST_SHIPMENT, PAYOR_LIST_PACKAGE }
    public enum ReturnedRateType { PAYOR_ACCOUNT_SHIPMENT, PAYOR_ACCOUNT_PACKAGE, PAYOR_LIST_SHIPMENT, PAYOR_LIST_PACKAGE }
    public enum CodCollectionType { ANY, CASH, COMPANY_CHECK, GUARANTEED_FUNDS, PERSONAL_CHECK }
}

// Track service stubs
namespace Nop.Plugin.Shipping.Fedex
{
    public class TrackService : IDisposable
    {
        public TrackService(string url) { }
        public TrackReply track(TrackRequest request) { return new TrackReply { HighestSeverity = RateServiceWebReference.NotificationSeverityType.SUCCESS, TrackDetails = new TrackDetail[0] }; }
        public void Dispose() { }
    }

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
        public bool TimestampSpecified { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventDescription { get; set; }
        public string EventType { get; set; }
        public TrackEventAddress Address { get; set; }
    }

    public class TrackEventAddress
    {
        public string City { get; set; }
        public string CountryCode { get; set; }
        public string StateOrProvinceCode { get; set; }
        public string PostalCode { get; set; }
    }

    public class TrackPackageIdentifier
    {
        public string Value { get; set; }
        public TrackIdentifierType Type { get; set; }
    }

    public enum TrackIdentifierType { TRACKING_NUMBER_OR_DOORTAG, SHIPPER_REFERENCE }

    public class SoapException : Exception
    {
        public System.Xml.XmlElement Detail { get; set; }
        public SoapException() { }
        public SoapException(string message) : base(message) { }
    }
}
