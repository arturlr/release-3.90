using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Nop.Services.EuropaCheckVatService
{
    /// <summary>
    /// Match code enumeration from the VIES service
    /// </summary>
    [XmlType(Namespace = "urn:ec.europa.eu:taxud:vies:services:checkVat:types")]
    public enum matchCode
    {
        /// <remarks/>
        [XmlEnum("1")]
        Item1,
        /// <remarks/>
        [XmlEnum("2")]
        Item2,
        /// <remarks/>
        [XmlEnum("3")]
        Item3,
    }

    /// <summary>
    /// VIES VAT check service client - replaces the old SOAP web reference
    /// Uses HttpClient to call the EU VIES SOAP service directly
    /// </summary>
    public partial class checkVatService : IDisposable
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private string _url;

        public checkVatService()
        {
            _url = "https://ec.europa.eu/taxation_customs/vies/services/checkVatService";
        }

        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }

        /// <summary>
        /// Check VAT number validity
        /// </summary>
        public DateTime checkVat(ref string countryCode, ref string vatNumber, out bool valid, out string name, out string address)
        {
            var soapRequest = string.Format(
                @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:ec.europa.eu:taxud:vies:services:checkVat:types"">
                    <soapenv:Header/>
                    <soapenv:Body>
                        <urn:checkVat>
                            <urn:countryCode>{0}</urn:countryCode>
                            <urn:vatNumber>{1}</urn:vatNumber>
                        </urn:checkVat>
                    </soapenv:Body>
                </soapenv:Envelope>",
                System.Net.WebUtility.HtmlEncode(countryCode),
                System.Net.WebUtility.HtmlEncode(vatNumber));

            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            var response = _httpClient.PostAsync(_url, content).GetAwaiter().GetResult();
            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // Parse the SOAP response
            var doc = new XmlDocument();
            doc.LoadXml(responseString);

            var nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsMgr.AddNamespace("ns2", "urn:ec.europa.eu:taxud:vies:services:checkVat:types");

            var responseNode = doc.SelectSingleNode("//ns2:checkVatResponse", nsMgr);
            if (responseNode == null)
            {
                // Try to find fault
                var faultNode = doc.SelectSingleNode("//soap:Fault/faultstring", nsMgr);
                var faultMessage = faultNode != null ? faultNode.InnerText : "Unknown VIES service error";
                throw new Exception("VIES service returned an error: " + faultMessage);
            }

            countryCode = GetNodeValue(responseNode, "ns2:countryCode", nsMgr) ?? countryCode;
            vatNumber = GetNodeValue(responseNode, "ns2:vatNumber", nsMgr) ?? vatNumber;
            valid = string.Equals(GetNodeValue(responseNode, "ns2:valid", nsMgr), "true", StringComparison.OrdinalIgnoreCase);
            name = GetNodeValue(responseNode, "ns2:name", nsMgr);
            address = GetNodeValue(responseNode, "ns2:address", nsMgr);

            var requestDateStr = GetNodeValue(responseNode, "ns2:requestDate", nsMgr);
            DateTime requestDate;
            if (!DateTime.TryParse(requestDateStr, out requestDate))
                requestDate = DateTime.UtcNow;

            return requestDate;
        }

        /// <summary>
        /// Check VAT number validity with approximate matching
        /// </summary>
        public DateTime checkVatApprox(
            ref string countryCode,
            ref string vatNumber,
            ref string traderName,
            ref string traderCompanyType,
            ref string traderStreet,
            ref string traderPostcode,
            ref string traderCity,
            string requesterCountryCode,
            string requesterVatNumber,
            out bool valid,
            out string traderAddress,
            out matchCode traderNameMatch,
            out bool traderNameMatchSpecified,
            out matchCode traderCompanyTypeMatch,
            out bool traderCompanyTypeMatchSpecified,
            out matchCode traderStreetMatch,
            out bool traderStreetMatchSpecified,
            out matchCode traderPostcodeMatch,
            out bool traderPostcodeMatchSpecified,
            out matchCode traderCityMatch,
            out bool traderCityMatchSpecified,
            out string requestIdentifier)
        {
            var soapRequest = string.Format(
                @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:ec.europa.eu:taxud:vies:services:checkVat:types"">
                    <soapenv:Header/>
                    <soapenv:Body>
                        <urn:checkVatApprox>
                            <urn:countryCode>{0}</urn:countryCode>
                            <urn:vatNumber>{1}</urn:vatNumber>
                            <urn:traderName>{2}</urn:traderName>
                            <urn:traderCompanyType>{3}</urn:traderCompanyType>
                            <urn:traderStreet>{4}</urn:traderStreet>
                            <urn:traderPostcode>{5}</urn:traderPostcode>
                            <urn:traderCity>{6}</urn:traderCity>
                            <urn:requesterCountryCode>{7}</urn:requesterCountryCode>
                            <urn:requesterVatNumber>{8}</urn:requesterVatNumber>
                        </urn:checkVatApprox>
                    </soapenv:Body>
                </soapenv:Envelope>",
                System.Net.WebUtility.HtmlEncode(countryCode ?? ""),
                System.Net.WebUtility.HtmlEncode(vatNumber ?? ""),
                System.Net.WebUtility.HtmlEncode(traderName ?? ""),
                System.Net.WebUtility.HtmlEncode(traderCompanyType ?? ""),
                System.Net.WebUtility.HtmlEncode(traderStreet ?? ""),
                System.Net.WebUtility.HtmlEncode(traderPostcode ?? ""),
                System.Net.WebUtility.HtmlEncode(traderCity ?? ""),
                System.Net.WebUtility.HtmlEncode(requesterCountryCode ?? ""),
                System.Net.WebUtility.HtmlEncode(requesterVatNumber ?? ""));

            var content = new StringContent(soapRequest, Encoding.UTF8, "text/xml");
            var response = _httpClient.PostAsync(_url, content).GetAwaiter().GetResult();
            var responseString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var doc = new XmlDocument();
            doc.LoadXml(responseString);

            var nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsMgr.AddNamespace("ns2", "urn:ec.europa.eu:taxud:vies:services:checkVat:types");

            var responseNode = doc.SelectSingleNode("//ns2:checkVatApproxResponse", nsMgr);
            if (responseNode == null)
            {
                var faultNode = doc.SelectSingleNode("//soap:Fault/faultstring", nsMgr);
                var faultMessage = faultNode != null ? faultNode.InnerText : "Unknown VIES service error";
                throw new Exception("VIES service returned an error: " + faultMessage);
            }

            countryCode = GetNodeValue(responseNode, "ns2:countryCode", nsMgr) ?? countryCode;
            vatNumber = GetNodeValue(responseNode, "ns2:vatNumber", nsMgr) ?? vatNumber;
            traderName = GetNodeValue(responseNode, "ns2:traderName", nsMgr) ?? traderName;
            traderCompanyType = GetNodeValue(responseNode, "ns2:traderCompanyType", nsMgr) ?? traderCompanyType;
            traderStreet = GetNodeValue(responseNode, "ns2:traderStreet", nsMgr) ?? traderStreet;
            traderPostcode = GetNodeValue(responseNode, "ns2:traderPostcode", nsMgr) ?? traderPostcode;
            traderCity = GetNodeValue(responseNode, "ns2:traderCity", nsMgr) ?? traderCity;
            valid = string.Equals(GetNodeValue(responseNode, "ns2:valid", nsMgr), "true", StringComparison.OrdinalIgnoreCase);
            traderAddress = GetNodeValue(responseNode, "ns2:traderAddress", nsMgr);
            requestIdentifier = GetNodeValue(responseNode, "ns2:requestIdentifier", nsMgr);

            traderNameMatch = ParseMatchCode(GetNodeValue(responseNode, "ns2:traderNameMatch", nsMgr), out traderNameMatchSpecified);
            traderCompanyTypeMatch = ParseMatchCode(GetNodeValue(responseNode, "ns2:traderCompanyTypeMatch", nsMgr), out traderCompanyTypeMatchSpecified);
            traderStreetMatch = ParseMatchCode(GetNodeValue(responseNode, "ns2:traderStreetMatch", nsMgr), out traderStreetMatchSpecified);
            traderPostcodeMatch = ParseMatchCode(GetNodeValue(responseNode, "ns2:traderPostcodeMatch", nsMgr), out traderPostcodeMatchSpecified);
            traderCityMatch = ParseMatchCode(GetNodeValue(responseNode, "ns2:traderCityMatch", nsMgr), out traderCityMatchSpecified);

            var requestDateStr = GetNodeValue(responseNode, "ns2:requestDate", nsMgr);
            DateTime requestDate;
            if (!DateTime.TryParse(requestDateStr, out requestDate))
                requestDate = DateTime.UtcNow;

            return requestDate;
        }

        private static string GetNodeValue(XmlNode parent, string xpath, XmlNamespaceManager nsMgr)
        {
            var node = parent.SelectSingleNode(xpath, nsMgr);
            return node?.InnerText;
        }

        private static matchCode ParseMatchCode(string value, out bool specified)
        {
            specified = false;
            if (string.IsNullOrEmpty(value))
                return matchCode.Item1;

            specified = true;
            switch (value)
            {
                case "1": return matchCode.Item1;
                case "2": return matchCode.Item2;
                case "3": return matchCode.Item3;
                default:
                    specified = false;
                    return matchCode.Item1;
            }
        }

        public void Dispose()
        {
            // HttpClient is static and shared, no need to dispose
        }
    }
}
