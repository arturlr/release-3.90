using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication.External;

namespace Nop.Plugin.ExternalAuth.Facebook.Core
{
    public class FacebookProviderAuthorizer : IOAuthProviderFacebookAuthorizer
    {
        #region Fields

        private const string FacebookAuthenticationScheme = "Facebook";

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

        private async Task<string> RequestEmailFromFacebook(string accessToken)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetStringAsync(
                    "https://graph.facebook.com/me?fields=email&access_token=" + Uri.EscapeDataString(accessToken));
                var userInfo = JObject.Parse(response);
                if (userInfo["email"] != null)
                {
                    return userInfo["email"].ToString();
                }
            }
            return string.Empty;
        }

        private async Task<AuthorizeState> VerifyAuthenticationAsync(string returnUrl)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var authenticateResult = await httpContext.AuthenticateAsync(FacebookAuthenticationScheme);

            if (authenticateResult.Succeeded && authenticateResult.Principal != null)
            {
                var principal = authenticateResult.Principal;
                var externalId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(externalId))
                    throw new Exception("Authentication result does not contain id data");

                var accessToken = await httpContext.GetTokenAsync("access_token") ?? string.Empty;

                var parameters = new OAuthAuthenticationParameters(Provider.SystemName)
                {
                    ExternalIdentifier = externalId,
                    OAuthToken = accessToken,
                    OAuthAccessToken = externalId,
                };

                if (_externalAuthenticationSettings.AutoRegisterEnabled)
                    await ParseClaimsAsync(principal, parameters, accessToken);

                var result = _authorizer.Authorize(parameters);

                return new AuthorizeState(returnUrl, result);
            }

            var state = new AuthorizeState(returnUrl, OpenAuthenticationStatus.Error);
            var error = authenticateResult.Failure?.Message ?? "Unknown error";
            state.AddError(error);
            return state;
        }

        private async Task ParseClaimsAsync(ClaimsPrincipal principal, OAuthAuthenticationParameters parameters, string accessToken)
        {
            var claims = new UserClaims();
            claims.Contact = new ContactClaims();

            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email))
            {
                claims.Contact.Email = email;
            }
            else
            {
                // Request email from Facebook Graph API
                claims.Contact.Email = await RequestEmailFromFacebook(accessToken);
            }

            claims.Name = new NameClaims();
            var name = principal.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrEmpty(name))
            {
                var nameSplit = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

        private AuthorizeState RequestAuthentication(string returnUrl)
        {
            var callbackUrl = string.Format("{0}plugins/externalauthFacebook/logincallback/", _webHelper.GetStoreLocation());
            if (!string.IsNullOrEmpty(returnUrl))
            {
                callbackUrl += "?returnUrl=" + Uri.EscapeDataString(returnUrl);
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = callbackUrl
            };

            return new AuthorizeState("", OpenAuthenticationStatus.RequiresRedirect)
            {
                Result = new ChallengeResult(FacebookAuthenticationScheme, properties)
            };
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
                return VerifyAuthenticationAsync(returnUrl).GetAwaiter().GetResult();

            return RequestAuthentication(returnUrl);
        }

        #endregion
    }
}
