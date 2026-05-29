#pragma warning disable 1591

using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace Nop.Services.EuropaCheckVatService
{
    /// <summary>
    /// VIES VAT check service client (replaces SOAP proxy)
    /// </summary>
    public partial class checkVatService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private const string ServiceUrl = "https://ec.europa.eu/taxation_customs/vies/services/checkVatService";

        public checkVatService()
        {
            _httpClient = new HttpClient();
        }

        public DateTime checkVat(ref string countryCode, ref string vatNumber, out bool valid, out string name, out string address)
        {
            var soapEnvelope = string.Format(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:urn=""urn:ec.europa.eu:taxud:vies:services:checkVat:types"">
   <soapenv:Header/>
   <soapenv:Body>
      <urn:checkVat>
         <urn:countryCode>{0}</urn:countryCode>
         <urn:vatNumber>{1}</urn:vatNumber>
      </urn:checkVat>
   </soapenv:Body>
</soapenv:Envelope>", countryCode, vatNumber);

            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            var response = _httpClient.PostAsync(ServiceUrl, content).GetAwaiter().GetResult();
            var responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var doc = XDocument.Parse(responseContent);
            XNamespace ns = "urn:ec.europa.eu:taxud:vies:services:checkVat:types";

            var body = doc.Descendants(ns + "checkVatResponse");
            valid = false;
            name = null;
            address = null;
            var requestDate = DateTime.UtcNow;

            foreach (var elem in body)
            {
                countryCode = elem.Element(ns + "countryCode")?.Value ?? countryCode;
                vatNumber = elem.Element(ns + "vatNumber")?.Value ?? vatNumber;
                var validStr = elem.Element(ns + "valid")?.Value;
                valid = string.Equals(validStr, "true", StringComparison.OrdinalIgnoreCase);
                name = elem.Element(ns + "name")?.Value;
                address = elem.Element(ns + "address")?.Value;
                var dateStr = elem.Element(ns + "requestDate")?.Value;
                if (!string.IsNullOrEmpty(dateStr))
                    DateTime.TryParse(dateStr, out requestDate);
            }

            return requestDate;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <remarks/>
    public enum matchCode
    {
        Item1,
        Item2,
        Item3,
    }
}

#pragma warning restore 1591
