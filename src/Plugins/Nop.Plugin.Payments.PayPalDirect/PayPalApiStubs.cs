using System.Collections.Generic;
// Stub types for PayPal.Api SDK - migration pending to PayPal REST SDK v2
// These stubs allow the plugin to compile while PayPal SDK migration is in progress

using System.Collections.Generic;

namespace PayPal.Api
{
    public class Item
    {
        public string name { get; set; }
        public string price { get; set; }
        public string currency { get; set; }
        public string quantity { get; set; }
        public string sku { get; set; }
    }

    public class APIContext
    {
        public APIContext(string accessToken) { }
        public Dictionary<string, string> Config { get; set; }
        public Dictionary<string, string> HTTPHeaders { get; set; }
    }

    public class OAuthTokenCredential
    {
        public OAuthTokenCredential(string clientId, string clientSecret, Dictionary<string, string> config) { }
        public OAuthTokenCredential(Dictionary<string, string> config) { }
        public string GetAccessToken() { return string.Empty; }
    }

    public class Payment
    {
        public string id { get; set; }
        public string state { get; set; }
        public Payer payer { get; set; }
        public List<Transaction> transactions { get; set; }
        public RedirectUrls redirect_urls { get; set; }
        public string intent { get; set; }
        public Payment Create(APIContext context) { return this; }
        public static Payment Get(APIContext context, string paymentId) { return new Payment(); }
        public List<Links> links { get; set; }
    }

    public class Payer
    {
        public string payment_method { get; set; }
        public PayerInfo payer_info { get; set; }
        public System.Collections.Generic.List<FundingInstrument> funding_instruments { get; set; }
    }

    public class PayerInfo
    {
        public string email { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public Address billing_address { get; set; }
    }

    public class FundingInstrument
    {
        public CreditCard credit_card { get; set; }
    }

    public class CreditCard
    {
        public string type { get; set; }
        public string number { get; set; }
        public int expire_month { get; set; }
        public int expire_year { get; set; }
        public string cvv2 { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public Address billing_address { get; set; }
    }

    public class Address
    {
        public string line1 { get; set; }
        public string line2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postal_code { get; set; }
        public string country_code { get; set; }
        public string phone { get; set; }
    }

    public class Transaction
    {
        public Amount amount { get; set; }
        public ItemList item_list { get; set; }
        public string invoice_number { get; set; }
        public string description { get; set; }
        public List<RelatedResources> related_resources { get; set; }
    }

    public class Amount
    {
        public string currency { get; set; }
        public string total { get; set; }
        public Details details { get; set; }
    }

    public class Details
    {
        public string tax { get; set; }
        public string shipping { get; set; }
        public string subtotal { get; set; }
        public string shipping_discount { get; set; }
    }

    public class ItemList
    {
        public List<Item> items { get; set; }
        public ShippingAddress shipping_address { get; set; }
    }

    public class ShippingAddress
    {
        public string recipient_name { get; set; }
        public string line1 { get; set; }
        public string line2 { get; set; }
        public string phone { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postal_code { get; set; }
        public string country_code { get; set; }
    }

    public class RedirectUrls
    {
        public string return_url { get; set; }
        public string cancel_url { get; set; }
    }

    public class Links
    {
        public string href { get; set; }
        public string rel { get; set; }
    }

    public class RelatedResources
    {
        public Sale sale { get; set; }
        public Authorization authorization { get; set; }
    }

    public class ProcessorResponse
    {
        public string avs_code { get; set; }
        public string cvv_code { get; set; }
    }

    public class Sale
    {
        public string id { get; set; }
        public string state { get; set; }
        public string billing_agreement_id { get; set; }
        public string invoice_number { get; set; }
        public ProcessorResponse processor_response { get; set; }
        public FmfDetails fmf_details { get; set; }
        public static Sale Get(APIContext context, string saleId) { return new Sale(); }
        public Refund RefundSale(APIContext context, RefundRequest refund) { return new Refund(); }
        public DetailedRefund RefundSale(APIContext context, Refund refund) { return new DetailedRefund(); }
    }

    public class FmfDetails
    {
        public string filter_type { get; set; }
        public string filter_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }

    public class Authorization
    {
        public string id { get; set; }
        public string state { get; set; }
        public FmfDetails fmf_details { get; set; }
        public static Authorization Get(APIContext context, string authId) { return new Authorization(); }
        public Capture Capture(APIContext context, Capture capture) { return new Capture(); }
        public Authorization Void(APIContext context) { return this; }
    }

    public class Capture
    {
        public Amount amount { get; set; }
        public bool is_final_capture { get; set; }
        public string id { get; set; }
        public string state { get; set; }
        public static Capture Get(APIContext context, string captureId) { return new Capture(); }
        public static DetailedRefund Refund(APIContext context, string captureId, Refund refund) { return new DetailedRefund(); }
    }

    public class Refund
    {
        public string id { get; set; }
        public Amount amount { get; set; }
    }

    public class RefundRequest : Refund
    {
        public Amount amount { get; set; }
    }

    public class DetailedRefund : Refund
    {
    }

    public class PaymentExecution
    {
        public string payer_id { get; set; }
        public List<Transaction> transactions { get; set; }
    }

    public class Plan
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public string state { get; set; }
        public List<PaymentDefinition> payment_definitions { get; set; }
        public MerchantPreferences merchant_preferences { get; set; }
        public Plan Create(APIContext context) { return this; }
        public void Update(APIContext context, PatchRequest request) { }
    }

    public class PaymentDefinition
    {
        public string name { get; set; }
        public string type { get; set; }
        public Currency amount { get; set; }
        public string frequency { get; set; }
        public string frequency_interval { get; set; }
        public string cycles { get; set; }
    }

    public class Money
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class Currency
    {
        public string value { get; set; }
        public string currency { get; set; }
    }

    public class MerchantPreferences
    {
        public string cancel_url { get; set; }
        public string return_url { get; set; }
        public string auto_bill_amount { get; set; }
        public Currency setup_fee { get; set; }
    }

    public class Agreement
    {
        public string name { get; set; }
        public string description { get; set; }
        public string start_date { get; set; }
        public Plan plan { get; set; }
        public Payer payer { get; set; }
        public string id { get; set; }
        public string token { get; set; }
        public ShippingAddress shipping_address { get; set; }
        public AgreementDetails agreement_details { get; set; }
        public Money BillBalance(APIContext context, AgreementStateDescriptor descriptor = null) { return new Money(); }
        public Agreement Create(APIContext context) { return this; }
        public static Agreement Execute(APIContext context, string token) { return new Agreement(); }
        public static Agreement Get(APIContext context, string id) { return new Agreement(); }
        public void Suspend(APIContext context, AgreementStateDescriptor descriptor) { }
        public void Cancel(APIContext context, AgreementStateDescriptor descriptor) { }
        public List<Links> links { get; set; }
    }

    public class AgreementDetails
    {
        public Money outstanding_balance { get; set; }
        public string cycles_remaining { get; set; }
        public string cycles_completed { get; set; }
        public string next_billing_date { get; set; }
        public string last_payment_date { get; set; }
        public Money last_payment_amount { get; set; }
    }

    public class AgreementStateDescriptor
    {
        public string note { get; set; }
        public Money amount { get; set; }
    }

    public class Patch
    {
        public string op { get; set; }
        public string path { get; set; }
        public object value { get; set; }
    }

    public class PatchRequest : List<Patch>
    {
    }

    public class PayPalResource
    {
    }

    public static class JsonFormatter
    {
        public static T ConvertFromJson<T>(string json) { return default(T); }
    }

    public class WebhookEvent
    {
        public string event_type { get; set; }
        public string resource_type { get; set; }
        public object resource { get; set; }
        public string summary { get; set; }
        public static WebhookEvent Get(APIContext context, string id) { return new WebhookEvent(); }
        public static bool ValidateReceivedEvent(APIContext context, Microsoft.AspNetCore.Http.IHeaderDictionary headers, string requestBody, string webhookId) { return true; }
    }

    public class WebhookEventType
    {
        public string name { get; set; }
    }

    public class Webhook
    {
        public string id { get; set; }
        public string url { get; set; }
        public System.Collections.Generic.List<WebhookEventType> event_types { get; set; }
        public Webhook Create(APIContext context) { return this; }
        public static Webhook Get(APIContext context, string id) { return new Webhook(); }
        public static void Delete(APIContext context, string id) { }
    }

    public class Error
    {
        public string name { get; set; }
        public string message { get; set; }
        public System.Collections.Generic.List<ErrorDetails> details { get; set; }
    }

    public class ErrorDetails
    {
        public string field { get; set; }
        public string issue { get; set; }
    }

}

namespace PayPal
{
    public class PayPalException : System.Exception
    {
        public PayPalException(string message) : base(message) { }
        public PayPalException(string message, System.Exception inner) : base(message, inner) { }
    }

    public class ConnectionException : PayPalException
    {
        public string Response { get; set; }
        public ConnectionException(string message) : base(message) { }
        public ConnectionException(string message, System.Exception inner) : base(message, inner) { }
    }
}
