using System.Collections.Generic;
using PayPal.Api;

namespace Nop.Plugin.Payments.PayPalDirect
{
    /// <summary>
    /// Represents paypal helper
    /// </summary>
    public class PaypalHelper
    {
        #region Constants

        /// <summary>
        /// nopCommerce partner code
        /// </summary>
        private const string BN_CODE = "nopCommerce_SP";

        #endregion

        #region Methods

        /// <summary>
        /// Replacement for <c>System.Web.HttpRequestBase.IsLocal</c>, which has NO ASP.NET Core
        /// equivalent.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Task 12.3. <c>Views/Configure.cshtml</c> reads <c>Request.IsLocal</c> to decide
        /// whether to offer the "create webhook" button: PayPal cannot deliver a webhook to a
        /// machine it cannot reach, so on a developer machine the button is hidden and the
        /// webhook id is entered by hand. That behaviour is preserved exactly — this is a
        /// like-for-like replacement, not a redesign.
        /// </para>
        /// <para>
        /// The implementation applies the same two tests <c>System.Web</c> used, and is
        /// deliberately IDENTICAL to the one task 6.2 wrote for
        /// <c>Nop.Web.Framework.Seo.WwwRequirementAttribute</c> (the other
        /// <c>Request.IsLocal</c> site in the solution): the remote address is a loopback
        /// address, or the remote address equals the local address, with a null remote address
        /// treated as local because <c>System.Web</c> treated an in-process request that way.
        /// </para>
        /// <para>
        /// <b>It lives here, in the plugin, rather than being promoted into
        /// <c>Nop.Web.Framework</c>.</b> The framework copy is <c>private</c> to
        /// <c>WwwRequirementAttribute</c>, and promoting it would mean adding public surface to a
        /// gated project that another migration group is editing concurrently — for one caller.
        /// Duplicating ten lines is the smaller cost, and the duplication is stated here so a
        /// later task can consolidate the two deliberately. Deferral <b>12.3-1</b> records it.
        /// </para>
        /// </remarks>
        public static bool IsLocalRequest(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var connection = httpContext == null ? null : httpContext.Connection;
            if (connection == null)
                return false;

            var remoteIp = connection.RemoteIpAddress;
            if (remoteIp == null)
            {
                //no remote address at all - System.Web treated an in-process request as local
                return true;
            }

            if (System.Net.IPAddress.IsLoopback(remoteIp))
                return true;

            var localIp = connection.LocalIpAddress;
            return localIp != null && remoteIp.Equals(localIp);
        }

        /// <summary>
        /// Get PayPal Api context 
        /// </summary>
        /// <param name="paypalDirectPaymentSettings">PayPalDirectPayment settings</param>
        /// <returns>ApiContext</returns>
        public static APIContext GetApiContext(PayPalDirectPaymentSettings payPalDirectPaymentSettings)
        {
            var mode = payPalDirectPaymentSettings.UseSandbox ? "sandbox" : "live";

            var config = new Dictionary<string, string>
            {
                { "clientId", payPalDirectPaymentSettings.ClientId },
                { "clientSecret", payPalDirectPaymentSettings.ClientSecret },
                { "mode", mode }
            };

            var accessToken = new OAuthTokenCredential(config).GetAccessToken();
            var apiContext = new APIContext(accessToken) { Config = config };

            if (apiContext.HTTPHeaders == null)
                apiContext.HTTPHeaders = new Dictionary<string, string>();
            apiContext.HTTPHeaders["PayPal-Partner-Attribution-Id"] = BN_CODE;

            return apiContext;
        }

        #endregion
    }
}

