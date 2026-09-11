using System;
using System.Collections.Generic;

namespace Nop.Plugin.ExternalAuth.Facebook.Core
{
    /// <summary>
    /// The outcome of verifying a Facebook OAuth 2 callback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TASK 11.1.</b> This replaces <c>DotNetOpenAuth.AspNet.AuthenticationResult</c>, which
    /// has no net10.0 build (see the remarks on <see cref="FacebookProviderAuthorizer"/>). The
    /// shape is the subset of that type this plugin actually consumed:
    /// <c>IsSuccessful</c>, <c>Error</c>, <c>ProviderUserId</c> and the loosely-typed
    /// <c>ExtraData</c> dictionary that <c>FacebookProviderAuthorizer.ParseClaims</c> reads
    /// <c>"username"</c>, <c>"name"</c> and <c>"accesstoken"</c> out of.
    /// </para>
    /// <para>
    /// <c>UserName</c> and <c>Provider</c> are deliberately NOT reproduced: DotNetOpenAuth set
    /// them and nothing in this plugin ever read them (<c>Provider.SystemName</c> is used
    /// instead, and the display name comes out of <c>ExtraData</c>). Recreating unread members
    /// would suggest they carry meaning here.
    /// </para>
    /// <para>
    /// The dictionary is deliberately <see cref="StringComparer.OrdinalIgnoreCase"/>: the Graph
    /// API's field names are lower-case, but 3.90's key literals are <c>"accesstoken"</c> (one
    /// word) against DotNetOpenAuth's own <c>"accesstoken"</c>, and being case-insensitive
    /// removes a class of silent lookup miss when a field is added later.
    /// </para>
    /// </remarks>
    public class FacebookAuthenticationResult
    {
        private FacebookAuthenticationResult(bool isSuccessful, string providerUserId,
            IDictionary<string, string> extraData, Exception error)
        {
            IsSuccessful = isSuccessful;
            ProviderUserId = providerUserId;
            ExtraData = extraData ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Error = error;
        }

        /// <summary>
        /// Whether the callback was verified and the provider returned a user
        /// </summary>
        public bool IsSuccessful { get; private set; }

        /// <summary>
        /// Facebook's opaque user identifier (the Graph API's <c>id</c> field)
        /// </summary>
        public string ProviderUserId { get; private set; }

        /// <summary>
        /// The provider's raw user fields, plus <c>accesstoken</c>
        /// </summary>
        public IDictionary<string, string> ExtraData { get; private set; }

        /// <summary>
        /// Why verification failed, when it did
        /// </summary>
        public Exception Error { get; private set; }

        public static FacebookAuthenticationResult Succeeded(string providerUserId,
            IDictionary<string, string> extraData)
        {
            return new FacebookAuthenticationResult(true, providerUserId, extraData, null);
        }

        public static FacebookAuthenticationResult Failed(string message)
        {
            return new FacebookAuthenticationResult(false, null, null, new Exception(message));
        }
    }
}
