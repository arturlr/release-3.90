using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Nop.Core.Infrastructure;

namespace Nop.Web.Infrastructure.Installation
{
    /// <summary>
    /// Localization service for installation
    /// </summary>
    public partial class InstallationLocalizationService : IInstallationLocalizationService
    {
        /// <summary>
        /// Cookie name to language for the installation page
        /// </summary>
        private const string LanguageCookieName = "nop.installation.lang";

        /// <summary>
        /// Available languages
        /// </summary>
        private IList<InstallationLanguage> _availableLanguages;

        /// <summary>
        /// Get the current language for the installation page
        /// </summary>
        /// <returns>Current language</returns>
        public virtual InstallationLanguage GetCurrentLanguage()
        {
            var httpContextAccessor = EngineContext.Current.Resolve<IHttpContextAccessor>();
            var httpContext = httpContextAccessor?.HttpContext;

            var cookieLanguageCode = "";
            if (httpContext != null)
            {
                httpContext.Request.Cookies.TryGetValue(LanguageCookieName, out cookieLanguageCode);
            }

            var availableLanguages = GetAvailableLanguages();

            var language = availableLanguages
                .FirstOrDefault(l => l.Code.Equals(cookieLanguageCode ?? "", StringComparison.InvariantCultureIgnoreCase));
            if (language != null)
                return language;

            // Try browser language
            if (httpContext != null)
            {
                var acceptLanguage = httpContext.Request.Headers["Accept-Language"].ToString();
                if (!string.IsNullOrEmpty(acceptLanguage))
                {
                    var userLanguage = acceptLanguage.Split(',').FirstOrDefault();
                    if (!string.IsNullOrEmpty(userLanguage))
                    {
                        language = availableLanguages
                            .FirstOrDefault(l => userLanguage.StartsWith(l.Code, StringComparison.InvariantCultureIgnoreCase));
                    }
                }
            }
            if (language != null)
                return language;

            language = availableLanguages.FirstOrDefault(l => l.IsDefault);
            if (language != null)
                return language;

            language = availableLanguages.FirstOrDefault();
            return language;
        }

        /// <summary>
        /// Save a language for the installation page
        /// </summary>
        /// <param name="languageCode">Language code</param>
        public virtual void SaveCurrentLanguage(string languageCode)
        {
            var httpContextAccessor = EngineContext.Current.Resolve<IHttpContextAccessor>();
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                httpContext.Response.Cookies.Append(LanguageCookieName, languageCode, new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddHours(24)
                });
            }
        }

        /// <summary>
        /// Get a list of available languages
        /// </summary>
        /// <returns>Result</returns>
        public virtual IList<InstallationLanguage> GetAvailableLanguages()
        {
            if (_availableLanguages == null)
            {
                _availableLanguages = new List<InstallationLanguage>();
                var webHelper = EngineContext.Current.Resolve<Nop.Core.IWebHelper>();
                foreach (var filePath in Directory.EnumerateFiles(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Localization", "Installation"), "*.xml"))
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(filePath);

                    var languageCode = "";
                    var languageName = "";
                    var isDefaultAttribute = xmlDoc.SelectSingleNode(@"//Language/@IsDefault");
                    var isRightToLeftAttribute = xmlDoc.SelectSingleNode(@"//Language/@IsRightToLeft");

                    var codeNode = xmlDoc.SelectSingleNode(@"//Language/@Code");
                    if (codeNode != null)
                        languageCode = codeNode.Value;

                    var nameNode = xmlDoc.SelectSingleNode(@"//Language/@Name");
                    if (nameNode != null)
                        languageName = nameNode.Value;

                    var isDefault = isDefaultAttribute != null && Convert.ToBoolean(isDefaultAttribute.Value);
                    var isRightToLeft = isRightToLeftAttribute != null && Convert.ToBoolean(isRightToLeftAttribute.Value);

                    var language = new InstallationLanguage
                    {
                        Code = languageCode,
                        Name = languageName,
                        IsDefault = isDefault,
                        IsRightToLeft = isRightToLeft,
                    };

                    // Load resources
                    foreach (XmlNode resNode in xmlDoc.SelectNodes(@"//Language/LocaleResource"))
                    {
                        var resNameAttribute = resNode.Attributes["Name"];
                        var resValueNode = resNode.SelectSingleNode("Value");

                        if (resNameAttribute == null)
                            continue;

                        var resourceName = resNameAttribute.Value.Trim();
                        var resourceValue = resValueNode != null ? resValueNode.InnerText.Trim() : "";

                        language.Resources.Add(new InstallationLocaleResource
                        {
                            Name = resourceName,
                            Value = resourceValue
                        });
                    }

                    _availableLanguages.Add(language);
                }

                _availableLanguages = _availableLanguages.OrderBy(l => l.Name).ToList();
            }
            return _availableLanguages;
        }

        /// <summary>
        /// Get a locale resource value
        /// </summary>
        /// <param name="resourceName">Resource name</param>
        /// <returns>Resource value</returns>
        public virtual string GetResource(string resourceName)
        {
            var language = GetCurrentLanguage();
            if (language == null)
                return resourceName;

            var resource = language.Resources.FirstOrDefault(r =>
                r.Name.Equals(resourceName, StringComparison.InvariantCultureIgnoreCase));
            if (resource == null)
                return resourceName;

            return resource.Value;
        }
    }
}
