using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                return string.Empty;

            var builder = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in args)
            {
                builder.Append(Uri.EscapeDataString(pair.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(pair.Value));
                builder.Append('&');
            }
            builder.Length--;
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
            {
                // OAuth verification pending migration to ASP.NET Core authentication
                throw new NotImplementedException("OAuth migration pending - use Microsoft.AspNetCore.Authentication.Facebook");
            }

            // Request authentication - redirect to Facebook
            var authUrl = GenerateServiceLoginUrl().AbsoluteUri;
            return new AuthorizeState("", OpenAuthenticationStatus.RequiresRedirect) { Result = new RedirectResult(authUrl) };
        }

        #endregion
    }
}
