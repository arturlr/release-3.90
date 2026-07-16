using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication.External;

namespace Nop.Plugin.ExternalAuth.Facebook.Core
{
    public class FacebookProviderAuthorizer : IOAuthProviderFacebookAuthorizer
    {
        #region Fields

        private readonly IExternalAuthorizer _authorizer;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly FacebookExternalAuthSettings _facebookExternalAuthSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public FacebookProviderAuthorizer(IExternalAuthorizer authorizer,
            ExternalAuthenticationSettings externalAuthenticationSettings,
            FacebookExternalAuthSettings facebookExternalAuthSettings,
            IHttpContextAccessor httpContextAccessor,
            IWebHelper webHelper)
        {
            this._authorizer = authorizer;
            this._externalAuthenticationSettings = externalAuthenticationSettings;
            this._facebookExternalAuthSettings = facebookExternalAuthSettings;
            this._httpContextAccessor = httpContextAccessor;
            this._webHelper = webHelper;
        }

        #endregion

        #region Utilities

        private string RequestEmailFromFacebook(string accessToken)
        {
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetStringAsync(
                    "https://graph.facebook.com/me?fields=email&access_token=" + EscapeUriDataStringRfc3986(accessToken))
                    .GetAwaiter().GetResult();
                var userInfo = JObject.Parse(response);
                if (userInfo["email"] != null)
                {
                    return userInfo["email"].ToString();
                }
            }
            return string.Empty;
        }

        private string ExchangeCodeForToken(string code)
        {
            using (var httpClient = new HttpClient())
            {
                var tokenUrl = string.Format(
                    "https://graph.facebook.com/v12.0/oauth/access_token?client_id={0}&redirect_uri={1}&client_secret={2}&code={3}",
                    EscapeUriDataStringRfc3986(_facebookExternalAuthSettings.ClientKeyIdentifier),
                    EscapeUriDataStringRfc3986(GenerateLocalCallbackUri().AbsoluteUri),
                    EscapeUriDataStringRfc3986(_facebookExternalAuthSettings.ClientSecret),
                    EscapeUriDataStringRfc3986(code));

                var response = httpClient.GetStringAsync(tokenUrl).GetAwaiter().GetResult();
                var tokenData = JObject.Parse(response);
                if (tokenData["access_token"] != null)
                {
                    return tokenData["access_token"].ToString();
                }
            }
            return null;
        }

        private AuthorizeState VerifyAuthentication(string returnUrl)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var code = httpContext.Request.Query["code"].FirstOrDefault();

            if (string.IsNullOrEmpty(code))
            {
                var state = new AuthorizeState(returnUrl, OpenAuthenticationStatus.Error);
                var error = httpContext.Request.Query["error_description"].FirstOrDefault() ?? "Unknown error";
                state.AddError(error);
                return state;
            }

            var accessToken = ExchangeCodeForToken(code);
            if (string.IsNullOrEmpty(accessToken))
            {
                var state = new AuthorizeState(returnUrl, OpenAuthenticationStatus.Error);
                state.AddError("Failed to obtain access token from Facebook");
                return state;
            }

            // Get user info
            string userId = null;
            string userName = null;
            string email = null;
            using (var httpClient = new HttpClient())
            {
                var meUrl = "https://graph.facebook.com/me?fields=id,name,email&access_token=" + EscapeUriDataStringRfc3986(accessToken);
                var meResponse = httpClient.GetStringAsync(meUrl).GetAwaiter().GetResult();
                var meData = JObject.Parse(meResponse);
                userId = meData["id"]?.ToString();
                userName = meData["name"]?.ToString();
                email = meData["email"]?.ToString();
            }

            if (string.IsNullOrEmpty(userId))
            {
                var state = new AuthorizeState(returnUrl, OpenAuthenticationStatus.Error);
                state.AddError("Authentication result does not contain id data");
                return state;
            }

            var parameters = new OAuthAuthenticationParameters(Provider.SystemName)
            {
                ExternalIdentifier = userId,
                OAuthToken = accessToken,
                OAuthAccessToken = userId,
            };

            if (_externalAuthenticationSettings.AutoRegisterEnabled)
            {
                var claims = new UserClaims();
                claims.Contact = new ContactClaims();
                claims.Contact.Email = email ?? RequestEmailFromFacebook(accessToken);
                claims.Name = new NameClaims();
                if (!string.IsNullOrEmpty(userName))
                {
                    var nameSplit = userName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                parameters.AddClaim(claims);
            }

            var result = _authorizer.Authorize(parameters);
            return new AuthorizeState(returnUrl, result);
        }

        private AuthorizeState RequestAuthentication()
        {
            var authUrl = GenerateServiceLoginUrl().AbsoluteUri;
            return new AuthorizeState("", OpenAuthenticationStatus.RequiresRedirect) { Result = new Microsoft.AspNetCore.Mvc.RedirectResult(authUrl) };
        }

        private Uri GenerateLocalCallbackUri()
        {
            string url = string.Format("{0}plugins/externalauthFacebook/logincallback/", _webHelper.GetStoreLocation());
            return new Uri(url);
        }

        private Uri GenerateServiceLoginUrl()
        {
            var builder = new UriBuilder("https://www.facebook.com/dialog/oauth");
            var args = new Dictionary<string, string>();
            args.Add("client_id", _facebookExternalAuthSettings.ClientKeyIdentifier);
            args.Add("redirect_uri", GenerateLocalCallbackUri().AbsoluteUri);
            args.Add("scope", "email");
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
        private readonly string[] UriRfc3986CharsToEscape = new string[] { "!", "*", "'", "(", ")" };
        private string EscapeUriDataStringRfc3986(string value)
        {
            var builder = new StringBuilder(Uri.EscapeDataString(value));
            for (int i = 0; i < UriRfc3986CharsToEscape.Length; i++)
            {
                builder.Replace(UriRfc3986CharsToEscape[i], Uri.HexEscape(UriRfc3986CharsToEscape[i][0]));
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
