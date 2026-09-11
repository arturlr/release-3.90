using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication.External;

namespace Nop.Plugin.ExternalAuth.Facebook.Core
{
    /// <summary>
    /// Drives Facebook's OAuth 2 authorization-code flow and hands the result to nopCommerce's
    /// <see cref="IExternalAuthorizer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ===================================================================================
    /// <b>TASK 11.1 — THE ONE PLUGIN WITH A DEAD THIRD-PARTY DEPENDENCY. READ THIS BEFORE
    /// CHANGING ANYTHING BELOW.</b>
    /// ===================================================================================
    /// 3.90 delegated the flow to <c>DotNetOpenAuth.AspNet.Clients.FacebookClient</c>
    /// (DotNetOpenAuth 4.3.4). There is <b>no net10.0 build of DotNetOpenAuth, no successor
    /// package, and the project is unmaintained</b> — its six assemblies were the only real
    /// third-party dependency in any of the 20 plugins, and they are deleted. The flow is
    /// implemented here instead.
    /// </para>
    /// <para>
    /// <b>WHY NOT <c>Microsoft.AspNetCore.Authentication.Facebook</c>, and a CORRECTION.</b> The
    /// task text recommended porting to it and described it as being "in the shared framework".
    /// <b>It is not</b> — measured against the net10.0 targeting pack: the shared framework ships
    /// <c>Microsoft.AspNetCore.Authentication.OAuth</c>, but the per-provider handlers
    /// (Facebook, Google, Twitter, MicrosoftAccount) are separate NuGet packages. So using it
    /// would be "substituting a package", which the task explicitly preferred to avoid. Three
    /// further reasons, each checked in this solution's own source rather than assumed:
    /// <list type="number">
    /// <item>It is a <b>startup-configured middleware handler</b>
    /// (<c>AddAuthentication().AddFacebook(o =&gt; …)</c>). This plugin's credentials are
    /// per-store <c>ISettings</c> rows read from the database at request time
    /// (<c>FacebookExternalAuthSettings</c>, loaded through <c>ISettingService</c> with a store
    /// scope), and the plugin can be installed or uninstalled without a restart. Bridging those
    /// requires an <c>IOptionsMonitor</c>/<c>PostConfigure</c> scheme <b>and</b> an edit to
    /// <c>Nop.Web</c>'s <c>Program.cs</c> — i.e. a host change driven by one plugin.</item>
    /// <item>It replaces, rather than implements, this migration's plugin contract.
    /// <c>IExternalAuthenticationMethod</c> + <c>IExternalProviderAuthorizer</c> +
    /// <c>AuthorizeState</c> are what nopCommerce 3.90 has and what task 6.2 deliberately kept
    /// (it changed only the <c>RouteValueDictionary</c> namespace on them). The handler model is
    /// what nopCommerce <b>4.x</b> moved to, together with a rewritten contract; adopting half
    /// of it would leave <c>DependencyRegistrar</c>,
    /// <c>IOAuthProviderFacebookAuthorizer</c> and <c>OAuthAuthenticationParameters</c> dead
    /// while the storefront button still routes through
    /// <c>ExternalAuthFacebookController.Login</c>.</item>
    /// <item>Its callback convention is <c>/signin-facebook</c>. 3.90's is
    /// <c>{store}/plugins/externalauthFacebook/logincallback/</c> — see
    /// <see cref="GenerateLocalCallbackUri"/> — and that exact string is registered in every
    /// existing deployment's Facebook app as a Valid OAuth Redirect URI. Changing it silently
    /// breaks every upgraded store until an administrator edits the Facebook app.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>HOW MUCH WAS ACTUALLY DELEGATED.</b> Less than the reference list suggests. 3.90 already
    /// hand-built the authorization URL itself — <see cref="GenerateServiceLoginUrl"/> carries
    /// 3.90's own comment <i>"code copied from DotNetOpenAuth.AspNet.Clients.FacebookClient
    /// file"</i>, along with copies of its <c>AppendQueryArgs</c>/<c>CreateQueryString</c>/
    /// <c>EscapeUriDataStringRfc3986</c> helpers — and it already called the Graph API directly
    /// in <see cref="RequestEmailFromFacebook"/>. The only genuinely delegated step was
    /// <c>FacebookClient.VerifyAuthentication</c>, i.e. <b>exchange <c>code</c> for a token, then
    /// GET <c>/me</c></b>. That is <see cref="VerifyAuthentication"/> plus
    /// <see cref="RequestAccessToken"/> and <see cref="RequestUserData"/> below, and it
    /// reproduces DotNetOpenAuth's observable contract: on success, <c>ExtraData</c> carries the
    /// provider's user fields plus an <c>accesstoken</c> entry, and <c>ProviderUserId</c> is the
    /// <c>id</c> field.
    /// </para>
    /// <para>
    /// ===================================================================================
    /// <b>ONE 3.90 DEFECT FIXED, BECAUSE IT IS LOAD-BEARING: THE TOKEN RESPONSE IS JSON.</b>
    /// ===================================================================================
    /// DotNetOpenAuth 4.3's <c>OAuth2Client.QueryAccessToken</c> parsed the token endpoint's
    /// response as <b>form-urlencoded</b> (<c>access_token=…&amp;expires=…</c>), which is what
    /// Facebook's Graph API returned up to v2.2. From <b>v2.3 (2015) Facebook returns JSON</b>
    /// and has done ever since, so the 3.90 code as written cannot obtain a token from any
    /// currently reachable Facebook endpoint — it would read an empty token and fail the login
    /// with "Cannot obtain access token". Reproducing that faithfully would mean porting a plugin
    /// that cannot work. <see cref="RequestAccessToken"/> therefore parses <b>JSON first and
    /// falls back to form-urlencoded</b>, so both response shapes are accepted and the fix
    /// cannot itself regress an old endpoint. Recorded as a behavioural change in
    /// runtime-deferrals.md section 91.1.
    /// </para>
    /// <para>
    /// ===================================================================================
    /// <b>TWO 3.90 BEHAVIOURS DELIBERATELY PRESERVED, ONE OF THEM ODD.</b>
    /// ===================================================================================
    /// <list type="number">
    /// <item><see cref="VerifyAuthentication"/> assigns
    /// <c>OAuthToken = ExtraData["accesstoken"]</c> but
    /// <c>OAuthAccessToken = authResult.ProviderUserId</c> — the <i>user id</i>, not the access
    /// token. That is 3.90's code, unchanged. It is almost certainly a copy-paste slip upstream,
    /// but it is not load-bearing (nopCommerce stores both columns on
    /// <c>ExternalAuthenticationRecord</c> and neither is used to call Facebook again), and
    /// "fix it" would silently change what an existing store has persisted for every linked
    /// account. Left alone and recorded.</item>
    /// <item>The Graph API removed the <c>username</c> field in v2.0, so
    /// <see cref="ParseClaims"/>'s first branch never fires against a live endpoint and the email
    /// always comes from <see cref="RequestEmailFromFacebook"/>. Both branches are kept exactly
    /// as 3.90 had them.</item>
    /// </list>
    /// </para>
    /// <para>
    /// ===================================================================================
    /// <b>API SUBSTITUTIONS (Requirement 4.2, 5.3)</b>
    /// ===================================================================================
    /// <list type="bullet">
    /// <item><c>System.Web.HttpContextBase</c> (injected, and passed to
    /// <c>VerifyAuthentication</c> so DotNetOpenAuth could read the query string) → the
    /// constructor parameter is <b>removed</b>; the two query-string reads go through
    /// <c>IWebHelper.QueryString&lt;string&gt;</c>, which task 2.4 re-based on
    /// <c>IHttpContextAccessor</c>. So the plugin uses the same accessor the rest of the
    /// migration does and needs no ASP.NET Core HTTP type of its own.</item>
    /// <item><c>System.Web.Mvc.RedirectResult</c> → <c>Microsoft.AspNetCore.Mvc.RedirectResult</c>;
    /// <c>AuthorizeState.Result</c> is already <c>Microsoft.AspNetCore.Mvc.ActionResult</c> after
    /// task 4.2.</item>
    /// <item><c>WebRequest.Create(...).GetResponse()</c> → a static
    /// <see cref="System.Net.Http.HttpClient"/>. <c>WebRequest</c> is obsolete on net10.0
    /// (<c>SYSLIB0014</c>) and this file was being rewritten anyway, so unlike
    /// <c>ExchangeRate.EcbExchange</c> (deferral 10.3-1) there was nothing to preserve by keeping
    /// it. One static instance, per the documented guidance — a per-call <c>HttpClient</c>
    /// exhausts sockets.</item>
    /// <item><c>Newtonsoft.Json</c> <c>JObject.Parse</c> → <see cref="System.Text.Json"/>, which
    /// is in the shared framework. Same substitution task 4.2 made in
    /// <c>ExternalAuthorizerHelper</c>, and it leaves this plugin with no
    /// <c>PackageReference</c> at all.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>NO CSRF <c>state</c> PARAMETER — 3.90 has none, and this port does not add one.</b>
    /// DotNetOpenAuth's <c>state</c>/<c>__sid__</c> handling lived in the
    /// <c>Microsoft.AspNet.WebPages.OAuth</c> layer that this plugin never used; 3.90's
    /// hand-built <see cref="GenerateServiceLoginUrl"/> sends no <c>state</c> and
    /// <see cref="VerifyAuthentication"/> validates none. Adding one is a behavioural change to
    /// a flow that cannot be exercised end to end without live Facebook credentials, so it is
    /// recorded as runtime deferral <b>11.1-2</b> rather than made silently here.
    /// </para>
    /// </remarks>
    public class FacebookProviderAuthorizer : IOAuthProviderFacebookAuthorizer
    {
        #region Constants

        /// <summary>
        /// Facebook's authorization dialog — 3.90's URL, unchanged
        /// </summary>
        private const string AuthorizationEndpoint = "https://www.facebook.com/dialog/oauth";

        /// <summary>
        /// The OAuth 2 token endpoint DotNetOpenAuth's FacebookClient used
        /// </summary>
        private const string TokenEndpoint = "https://graph.facebook.com/oauth/access_token";

        /// <summary>
        /// The Graph API "current user" endpoint DotNetOpenAuth's FacebookClient used
        /// </summary>
        private const string UserDataEndpoint = "https://graph.facebook.com/me";

        /// <summary>
        /// The scope 3.90 requested
        /// </summary>
        private const string Scope = "email";

        #endregion

        #region Fields

        /// <summary>
        /// One instance for the process. A per-call HttpClient leaks sockets in TIME_WAIT; the
        /// class is thread-safe for concurrent GETs and this plugin never mutates it.
        /// </summary>
        private static readonly HttpClient HttpClient = new HttpClient();

        private readonly IExternalAuthorizer _authorizer;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly FacebookExternalAuthSettings _facebookExternalAuthSettings;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public FacebookProviderAuthorizer(IExternalAuthorizer authorizer,
            ExternalAuthenticationSettings externalAuthenticationSettings,
            FacebookExternalAuthSettings facebookExternalAuthSettings,
            IWebHelper webHelper)
        {
            this._authorizer = authorizer;
            this._externalAuthenticationSettings = externalAuthenticationSettings;
            this._facebookExternalAuthSettings = facebookExternalAuthSettings;
            this._webHelper = webHelper;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// GET a URL and return the body, or null when the request failed.
        /// </summary>
        /// <remarks>
        /// Synchronous by intent: <see cref="IExternalProviderAuthorizer.Authorize"/> is a
        /// synchronous contract called from a controller action, so the wait has to happen
        /// somewhere. ASP.NET Core installs no <c>SynchronizationContext</c>, so this cannot
        /// deadlock — the same trade-off tasks 4.2 and 6.2 recorded for the other sync-over-async
        /// sites in this migration.
        /// </remarks>
        private static string Get(string url)
        {
            using (var response = HttpClient.GetAsync(url).GetAwaiter().GetResult())
            {
                if (!response.IsSuccessStatusCode)
                    return null;

                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Read a single string field out of a JSON object, or null.
        /// </summary>
        private static string ReadJsonString(JsonElement root, string name)
        {
            JsonElement value;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(name, out value))
                return null;

            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    return value.GetString();
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return value.GetRawText();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Ask the Graph API for the signed-in user's email address.
        /// </summary>
        /// <remarks>
        /// 3.90's method, with <c>WebRequest</c>/<c>JObject</c> replaced (see the class remarks).
        /// It is the fallback <see cref="ParseClaims"/> uses because the Graph API no longer
        /// returns a <c>username</c> field, and it returns <see cref="string.Empty"/> rather than
        /// throwing when the user has not granted the <c>email</c> permission — 3.90's behaviour,
        /// and what <c>ExternalAuthorizer</c> expects (it raises a "the provider did not return an
        /// email" error of its own).
        /// </remarks>
        private string RequestEmailFromFacebook(string accessToken)
        {
            var body = Get(UserDataEndpoint + "?fields=email&access_token="
                                            + EscapeUriDataStringRfc3986(accessToken));
            if (string.IsNullOrEmpty(body))
                return string.Empty;

            try
            {
                using (var document = JsonDocument.Parse(body))
                    return ReadJsonString(document.RootElement, "email") ?? string.Empty;
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Exchange the authorization <c>code</c> for an access token.
        /// </summary>
        /// <remarks>
        /// <b>Both response shapes are accepted — see "ONE 3.90 DEFECT FIXED" in the class
        /// remarks.</b> Graph API v2.3+ returns
        /// <c>{"access_token":"…","token_type":"bearer","expires_in":…}</c>; v2.2 and earlier
        /// returned <c>access_token=…&amp;expires=…</c>, which is what DotNetOpenAuth 4.3 parsed.
        /// JSON is tried first because it is what every reachable endpoint returns today.
        /// <para>
        /// The parameters, their order and the <c>scope</c> are DotNetOpenAuth's
        /// <c>FacebookClient.QueryAccessToken</c>, preserved: <c>client_id</c>,
        /// <c>redirect_uri</c>, <c>client_secret</c>, <c>code</c>, <c>scope</c>. The
        /// <c>redirect_uri</c> must be byte-identical to the one sent to the authorization
        /// endpoint or Facebook rejects the exchange, which is why both come from
        /// <see cref="GenerateLocalCallbackUri"/>.
        /// </para>
        /// </remarks>
        private string RequestAccessToken(string code)
        {
            var args = new Dictionary<string, string>
            {
                { "client_id", _facebookExternalAuthSettings.ClientKeyIdentifier },
                { "redirect_uri", GenerateLocalCallbackUri().AbsoluteUri },
                { "client_secret", _facebookExternalAuthSettings.ClientSecret },
                { "code", code },
                { "scope", Scope }
            };

            var builder = new UriBuilder(TokenEndpoint);
            AppendQueryArgs(builder, args);

            var body = Get(builder.Uri.AbsoluteUri);
            if (string.IsNullOrEmpty(body))
                return null;

            //Graph API v2.3+ - JSON
            try
            {
                using (var document = JsonDocument.Parse(body))
                {
                    var token = ReadJsonString(document.RootElement, "access_token");
                    if (!string.IsNullOrEmpty(token))
                        return token;
                }
            }
            catch (JsonException)
            {
                //not JSON - fall through to the legacy shape
            }

            //Graph API v2.2 and earlier - form-urlencoded, which is what DotNetOpenAuth parsed
            foreach (var pair in body.Split('&'))
            {
                var separator = pair.IndexOf('=');
                if (separator <= 0)
                    continue;

                if (!string.Equals(pair.Substring(0, separator), "access_token", StringComparison.Ordinal))
                    continue;

                var token = Uri.UnescapeDataString(pair.Substring(separator + 1));
                if (!string.IsNullOrEmpty(token))
                    return token;
            }

            return null;
        }

        /// <summary>
        /// GET the Graph API's <c>/me</c> and flatten it to the loosely-typed dictionary the rest
        /// of this plugin reads.
        /// </summary>
        /// <remarks>
        /// The field list is DotNetOpenAuth's <c>FacebookGraphData</c>: <c>id</c>, <c>name</c>,
        /// <c>link</c>, <c>username</c>, <c>gender</c>, <c>birthday</c>, <c>email</c>. Several no
        /// longer exist in the Graph API (<c>username</c> went in v2.0), so each is copied only
        /// when present — which is exactly how <see cref="ParseClaims"/>'s
        /// <c>ExtraData.ContainsKey</c> tests are written. <c>id</c> and <c>name</c> are NOT
        /// requested explicitly: <c>/me</c> returns them by default, as it did for
        /// DotNetOpenAuth.
        /// </remarks>
        private IDictionary<string, string> RequestUserData(string accessToken)
        {
            var body = Get(UserDataEndpoint + "?access_token=" + EscapeUriDataStringRfc3986(accessToken));
            if (string.IsNullOrEmpty(body))
                return null;

            try
            {
                using (var document = JsonDocument.Parse(body))
                {
                    var root = document.RootElement;

                    var id = ReadJsonString(root, "id");
                    if (string.IsNullOrEmpty(id))
                        return null;

                    var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var field in new[] { "id", "name", "link", "username", "gender", "birthday", "email" })
                    {
                        var value = ReadJsonString(root, field);
                        if (value != null)
                            data[field] = value;
                    }

                    return data;
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Verify the callback: read <c>code</c>, exchange it, load the user, and let
        /// nopCommerce's <see cref="IExternalAuthorizer"/> associate or register.
        /// </summary>
        /// <remarks>
        /// Structurally 3.90's method. What changed is only the first statement: instead of
        /// <c>FacebookApplication.VerifyAuthentication(_httpContext, GenerateLocalCallbackUri())</c>
        /// the three steps DotNetOpenAuth performed are inlined into
        /// <see cref="VerifyCallback"/>. The <c>ExtraData</c> guards, the
        /// <c>OAuthAuthenticationParameters</c> assignment (including the
        /// <c>OAuthAccessToken = ProviderUserId</c> oddity), the
        /// <c>AutoRegisterEnabled</c> test and both <see cref="AuthorizeState"/> shapes are
        /// unchanged.
        /// </remarks>
        private AuthorizeState VerifyAuthentication(string returnUrl)
        {
            var authResult = VerifyCallback();

            if (authResult.IsSuccessful)
            {
                if (!authResult.ExtraData.ContainsKey("id"))
                    throw new Exception("Authentication result does not contain id data");

                if (!authResult.ExtraData.ContainsKey("accesstoken"))
                    throw new Exception("Authentication result does not contain accesstoken data");

                var parameters = new OAuthAuthenticationParameters(Provider.SystemName)
                {
                    ExternalIdentifier = authResult.ProviderUserId,
                    OAuthToken = authResult.ExtraData["accesstoken"],
                    OAuthAccessToken = authResult.ProviderUserId,
                };

                if (_externalAuthenticationSettings.AutoRegisterEnabled)
                    ParseClaims(authResult, parameters);

                var result = _authorizer.Authorize(parameters);

                return new AuthorizeState(returnUrl, result);
            }

            var state = new AuthorizeState(returnUrl, OpenAuthenticationStatus.Error);
            var error = authResult.Error != null ? authResult.Error.Message : "Unknown error";
            state.AddError(error);
            return state;
        }

        /// <summary>
        /// The three steps <c>DotNetOpenAuth.AspNet.Clients.OAuth2Client.VerifyAuthentication</c>
        /// performed, in its order and with its failure messages.
        /// </summary>
        /// <remarks>
        /// One addition: Facebook reports a declined dialog by redirecting back with
        /// <c>error</c>/<c>error_description</c> and <b>no</b> <c>code</c>. DotNetOpenAuth ignored
        /// those and reported its generic "did not return a code" failure, so the user saw
        /// "Unknown error" on the login page after pressing Cancel. The description is surfaced
        /// instead when Facebook sends one — strictly more information in the branch that was
        /// already a failure, and the only place this method departs from DotNetOpenAuth's
        /// observable behaviour.
        /// </remarks>
        private FacebookAuthenticationResult VerifyCallback()
        {
            var code = _webHelper.QueryString<string>("code");
            if (string.IsNullOrEmpty(code))
            {
                var providerError = _webHelper.QueryString<string>("error_description");
                if (string.IsNullOrEmpty(providerError))
                    providerError = _webHelper.QueryString<string>("error");

                return FacebookAuthenticationResult.Failed(string.IsNullOrEmpty(providerError)
                    ? "Facebook did not return an authorization code"
                    : providerError);
            }

            var accessToken = RequestAccessToken(code);
            if (string.IsNullOrEmpty(accessToken))
                return FacebookAuthenticationResult.Failed("Cannot obtain access token from Facebook");

            var userData = RequestUserData(accessToken);
            if (userData == null)
                return FacebookAuthenticationResult.Failed("Cannot obtain user data from Facebook");

            //DotNetOpenAuth put the token into ExtraData under this exact key, and
            //VerifyAuthentication above requires it
            userData["accesstoken"] = accessToken;

            return FacebookAuthenticationResult.Succeeded(userData["id"], userData);
        }

        /// <summary>
        /// 3.90's claims mapping, unchanged apart from the result type.
        /// </summary>
        private void ParseClaims(FacebookAuthenticationResult authenticationResult, OAuthAuthenticationParameters parameters)
        {
            var claims = new UserClaims();
            claims.Contact = new ContactClaims();
            if (authenticationResult.ExtraData.ContainsKey("username"))
            {
                claims.Contact.Email = authenticationResult.ExtraData["username"];
            }
            else
            {
                //request email
                claims.Contact.Email = RequestEmailFromFacebook(authenticationResult.ExtraData["accesstoken"]);
            }
            claims.Name = new NameClaims();
            if (authenticationResult.ExtraData.ContainsKey("name"))
            {
                var name = authenticationResult.ExtraData["name"];
                if (!String.IsNullOrEmpty(name))
                {
                    var nameSplit = name.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nameSplit.Length >= 2)
                    {
                        claims.Name.First = nameSplit[0];
                        claims.Name.Last = nameSplit[1];
                    }
                    else
                    {
                        claims.Name.Last = nameSplit[0];
                    }
                }
            }

            parameters.AddClaim(claims);
        }

        private AuthorizeState RequestAuthentication()
        {
            var authUrl = GenerateServiceLoginUrl().AbsoluteUri;
            return new AuthorizeState("", OpenAuthenticationStatus.RequiresRedirect) { Result = new RedirectResult(authUrl) };
        }

        /// <summary>
        /// The URL Facebook redirects back to. 3.90's, character for character.
        /// </summary>
        /// <remarks>
        /// It must match <c>RouteProvider</c>'s
        /// <c>Plugins/ExternalAuthFacebook/LoginCallback</c> pattern (case-insensitively, which
        /// endpoint routing is) and it is also sent as <c>redirect_uri</c> to both the
        /// authorization and the token endpoint — Facebook compares the two, so they are built
        /// here once. Changing it invalidates every existing deployment's Facebook app
        /// configuration; see reason 3 in the class remarks.
        /// </remarks>
        private Uri GenerateLocalCallbackUri()
        {
            string url = string.Format("{0}plugins/externalauthFacebook/logincallback/", _webHelper.GetStoreLocation());
            return new Uri(url);
        }

        /// <summary>
        /// The authorization dialog URL. 3.90's, with its own comment kept.
        /// </summary>
        private Uri GenerateServiceLoginUrl()
        {
            //code copied from DotNetOpenAuth.AspNet.Clients.FacebookClient file
            var builder = new UriBuilder(AuthorizationEndpoint);
            var args = new Dictionary<string, string>();
            args.Add("client_id", _facebookExternalAuthSettings.ClientKeyIdentifier);
            args.Add("redirect_uri", GenerateLocalCallbackUri().AbsoluteUri);
            args.Add("scope", Scope);
            AppendQueryArgs(builder, args);
            return builder.Uri;
        }

        private void AppendQueryArgs(UriBuilder builder, IEnumerable<KeyValuePair<string, string>> args)
        {
            if (args != null && args.Any())
            {
                var builder2 = new StringBuilder();
                if (!string.IsNullOrEmpty(builder.Query))
                {
                    builder2.Append(builder.Query.Substring(1));
                    builder2.Append('&');
                }
                builder2.Append(CreateQueryString(args));
                builder.Query = builder2.ToString();
            }
        }
        private string CreateQueryString(IEnumerable<KeyValuePair<string, string>> args)
        {
            if (!args.Any())
            {
                return string.Empty;
            }
            var builder = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in args)
            {
                builder.Append(EscapeUriDataStringRfc3986(pair.Key));
                builder.Append('=');
                builder.Append(EscapeUriDataStringRfc3986(pair.Value));
                builder.Append('&');
            }
            builder.Length--;
            return builder.ToString();
        }
        private static readonly string[] UriRfc3986CharsToEscape = new string[] { "!", "*", "'", "(", ")" };

        /// <summary>
        /// 3.90's RFC 3986 escaping, with one API substitution.
        /// </summary>
        /// <remarks>
        /// <c>Uri.HexEscape(char)</c> is obsolete on net10.0 (<c>SYSLIB0013</c>) and is replaced
        /// by an invariant two-digit hex format of the same character, which produces the
        /// identical <c>%XX</c> string for the five ASCII characters in
        /// <see cref="UriRfc3986CharsToEscape"/>. Not a behavioural change; asserted by
        /// <c>Nop.Web.SmokeTests.PluginViewRenderTests</c> through the authorization URL it
        /// builds.
        /// </remarks>
        private string EscapeUriDataStringRfc3986(string value)
        {
            var builder = new StringBuilder(Uri.EscapeDataString(value ?? string.Empty));
            for (int i = 0; i < UriRfc3986CharsToEscape.Length; i++)
            {
                builder.Replace(UriRfc3986CharsToEscape[i],
                    "%" + ((int)UriRfc3986CharsToEscape[i][0]).ToString("X2",
                        System.Globalization.CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Authorize response
        /// </summary>
        /// <param name="returnUrl">Return URL</param>
        /// <param name="verifyResponse">true - Verify response;false - request authentication;null - determine automatically</param>
        /// <returns>Authorize state</returns>
        public AuthorizeState Authorize(string returnUrl, bool? verifyResponse = null)
        {
            if (!verifyResponse.HasValue)
                throw new ArgumentException("Facebook plugin cannot automatically determine verifyResponse property");

            if (verifyResponse.Value)
                return VerifyAuthentication(returnUrl);
            
            return RequestAuthentication();
        }

        #endregion
    }
}
