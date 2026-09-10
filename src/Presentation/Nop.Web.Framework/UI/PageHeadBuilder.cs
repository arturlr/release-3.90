using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Nop.Core;
using Nop.Core.Domain.Seo;

namespace Nop.Web.Framework.UI
{
    /// <summary>
    /// Page head builder
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bundling was dropped in the .NET 10 port (design §8, user-approved).</b>
    /// <c>System.Web.Optimization</c> (<c>BundleTable</c>, <c>ScriptBundle</c>,
    /// <c>StyleBundle</c>, <c>IBundleOrderer</c>, <c>CssRewriteUrlTransform</c>) and its
    /// minification back end (<c>WebGrease</c> + <c>Antlr</c>) are System.Web features with no
    /// .NET Core / .NET 10 release and no successor. <c>GenerateScripts</c> and
    /// <c>GenerateCssFiles</c> now emit one tag per registered asset, in registration order.
    /// </para>
    /// <para>
    /// <b>Cache busting replaces the one operationally useful property bundling had</b> — a
    /// fingerprinted URL that invalidated stale browser caches on deploy. Every emitted asset URL
    /// is stamped by <see cref="IFileVersionProvider"/>, the same service that backs the
    /// <c>asp-append-version</c> tag helper. The tag helper itself is unusable here because this
    /// class composes markup as strings rather than rendering tag helpers, so the
    /// <see cref="IFileVersionProvider.AddFileVersionToPath"/> seam is injected directly.
    /// </para>
    /// <para>
    /// <c>SeoSettings.EnableJsBundling</c> / <c>EnableCssBundling</c> are retained on the settings
    /// entity (no DB migration) but are never read here — they are inert. Likewise the
    /// <c>excludeFromBundle</c> / <c>bundleFiles</c> parameters, kept so no view call site changes.
    /// </para>
    /// <para>
    /// Removed from this class by the same decision: the <c>protected virtual</c> members
    /// <c>GetBundleVirtualPath(string, string, string[])</c> and <c>GetCssTranform()</c>, plus the
    /// private <c>s_lock</c> used to guard bundle registration. Both existed only to build and
    /// register bundles.
    /// </para>
    /// </remarks>
    public partial class PageHeadBuilder : IPageHeadBuilder
    {
        #region Fields

        private readonly SeoSettings _seoSettings;
        private readonly IFileVersionProvider _fileVersionProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly List<string> _titleParts;
        private readonly List<string> _metaDescriptionParts;
        private readonly List<string> _metaKeywordParts;
        private readonly Dictionary<ResourceLocation, List<ScriptReferenceMeta>> _scriptParts;
        private readonly Dictionary<ResourceLocation, List<CssReferenceMeta>> _cssParts;
        private readonly List<string> _canonicalUrlParts;
        private readonly List<string> _headCustomParts;
        private readonly List<string> _pageCssClassParts;
        private string _editPageUrl;
        private string _activeAdminMenuSystemName;

        #endregion

        #region Ctor

        /// <summary>
        /// Constuctor
        /// </summary>
        /// <param name="seoSettings">SEO settings</param>
        /// <param name="fileVersionProvider">
        /// Asset version provider used for cache busting. Registered by ASP.NET Core's MVC view
        /// services (<c>AddControllersWithViews()</c> / <c>AddRazorViewEngine()</c>); it needs
        /// <c>IWebHostEnvironment.WebRootFileProvider</c> and <c>IMemoryCache</c>, both of which
        /// the host already provides.
        /// </param>
        /// <param name="httpContextAccessor">
        /// Used only to read <c>Request.PathBase</c>, which
        /// <see cref="IFileVersionProvider.AddFileVersionToPath"/> requires in order to map a
        /// request URL back onto a web-root-relative file when the application is hosted in a
        /// virtual directory.
        /// </param>
        public PageHeadBuilder(SeoSettings seoSettings,
            IFileVersionProvider fileVersionProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            this._seoSettings = seoSettings;
            this._fileVersionProvider = fileVersionProvider;
            this._httpContextAccessor = httpContextAccessor;
            this._titleParts = new List<string>();
            this._metaDescriptionParts = new List<string>();
            this._metaKeywordParts = new List<string>();
            this._scriptParts = new Dictionary<ResourceLocation, List<ScriptReferenceMeta>>();
            this._cssParts = new Dictionary<ResourceLocation, List<CssReferenceMeta>>();
            this._canonicalUrlParts = new List<string>();
            this._headCustomParts = new List<string>();
            this._pageCssClassParts = new List<string>();
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Resolve an application-relative asset path to a URL and stamp a version suffix onto it.
        /// </summary>
        /// <remarks>
        /// Replaces bundling's fingerprinted URL. <see cref="IFileVersionProvider"/> returns the
        /// path unchanged when the file cannot be found under the web root, so CDN/absolute URLs
        /// and assets served from outside <c>wwwroot</c> pass through untouched rather than
        /// failing.
        /// </remarks>
        /// <param name="urlHelper">URL helper</param>
        /// <param name="part">Registered asset path (may be app-relative, e.g. <c>~/Scripts/x.js</c>)</param>
        /// <returns>Versioned URL</returns>
        protected virtual string GetAssetUrl(IUrlHelper urlHelper, string part)
        {
            var url = urlHelper != null ? urlHelper.Content(part) : part;

            if (_fileVersionProvider == null || string.IsNullOrEmpty(url))
                return url;

            var pathBase = _httpContextAccessor?.HttpContext?.Request.PathBase ?? PathString.Empty;

            return _fileVersionProvider.AddFileVersionToPath(pathBase, url);
        }

        #endregion

        #region Methods

        public virtual void AddTitleParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _titleParts.Add(part);
        }
        public virtual void AppendTitleParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
            
            _titleParts.Insert(0, part);
        }
        public virtual string GenerateTitle(bool addDefaultTitle)
        {
            string result = "";
            var specificTitle = string.Join(_seoSettings.PageTitleSeparator, _titleParts.AsEnumerable().Reverse().ToArray());
            if (!String.IsNullOrEmpty(specificTitle))
            {
                if (addDefaultTitle)
                {
                    //store name + page title
                    switch (_seoSettings.PageTitleSeoAdjustment)
                    {
                        case PageTitleSeoAdjustment.PagenameAfterStorename:
                            {
                                result = string.Join(_seoSettings.PageTitleSeparator, _seoSettings.DefaultTitle, specificTitle);
                            }
                            break;
                        case PageTitleSeoAdjustment.StorenameAfterPagename:
                        default:
                            {
                                result = string.Join(_seoSettings.PageTitleSeparator, specificTitle, _seoSettings.DefaultTitle);
                            }
                            break;
                            
                    }
                }
                else
                {
                    //page title only
                    result = specificTitle;
                }
            }
            else
            {
                //store name only
                result = _seoSettings.DefaultTitle;
            }
            return result;
        }


        public virtual void AddMetaDescriptionParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
            
            _metaDescriptionParts.Add(part);
        }
        public virtual void AppendMetaDescriptionParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
            
            _metaDescriptionParts.Insert(0, part);
        }
        public virtual string GenerateMetaDescription()
        {
            var metaDescription = string.Join(", ", _metaDescriptionParts.AsEnumerable().Reverse().ToArray());
            var result = !String.IsNullOrEmpty(metaDescription) ? metaDescription : _seoSettings.DefaultMetaDescription;
            return result;
        }


        public virtual void AddMetaKeywordParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
            
            _metaKeywordParts.Add(part);
        }
        public virtual void AppendMetaKeywordParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _metaKeywordParts.Insert(0, part);
        }
        public virtual string GenerateMetaKeywords()
        {
            var metaKeyword = string.Join(", ", _metaKeywordParts.AsEnumerable().Reverse().ToArray());
            var result = !String.IsNullOrEmpty(metaKeyword) ? metaKeyword : _seoSettings.DefaultMetaKeywords;
            return result;
        }
    

        /// <param name="excludeFromBundle">Recorded but INERT — bundling was dropped (design §8)</param>
        public virtual void AddScriptParts(ResourceLocation location, string part, bool excludeFromBundle, bool isAsync)
        {
            if (!_scriptParts.ContainsKey(location))
                _scriptParts.Add(location, new List<ScriptReferenceMeta>());

            if (string.IsNullOrEmpty(part))
                return;

            _scriptParts[location].Add(new ScriptReferenceMeta
            {
                ExcludeFromBundle = excludeFromBundle,
                IsAsync = isAsync,
                Part = part
            });
        }
        /// <param name="excludeFromBundle">Recorded but INERT — bundling was dropped (design §8)</param>
        public virtual void AppendScriptParts(ResourceLocation location, string part, bool excludeFromBundle, bool isAsync)
        {
            if (!_scriptParts.ContainsKey(location))
                _scriptParts.Add(location, new List<ScriptReferenceMeta>());

            if (string.IsNullOrEmpty(part))
                return;

            _scriptParts[location].Insert(0, new ScriptReferenceMeta
            {
                ExcludeFromBundle = excludeFromBundle,
                IsAsync = isAsync,
                Part = part
            });
        }
        /// <summary>
        /// Generate all script parts as individual &lt;script&gt; elements, in registration order.
        /// </summary>
        /// <param name="urlHelper">URL helper</param>
        /// <param name="location">A location of the script element</param>
        /// <param name="bundleFiles">IGNORED — bundling was dropped (design §8)</param>
        /// <returns>Generated string</returns>
        public virtual string GenerateScripts(IUrlHelper urlHelper, ResourceLocation location, bool? bundleFiles = null)
        {
            if (!_scriptParts.ContainsKey(location) || _scriptParts[location] == null)
                return "";

            if (!_scriptParts.Any())
                return "";

            //bundling has been dropped: every registered script is emitted as its own element,
            //with a cache-busting version suffix. "bundleFiles" and each part's
            //"ExcludeFromBundle" flag are deliberately not consulted.
            var result = new StringBuilder();
            foreach (var item in _scriptParts[location].Select(x => new { x.Part, x.IsAsync }).Distinct())
            {
                result.AppendFormat("<script {2}src=\"{0}\" type=\"{1}\"></script>", GetAssetUrl(urlHelper, item.Part), MimeTypes.TextJavascript, item.IsAsync ? "async " : "");
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }


        /// <param name="excludeFromBundle">Recorded but INERT — bundling was dropped (design §8)</param>
        public virtual void AddCssFileParts(ResourceLocation location, string part, bool excludeFromBundle = false)
        {
            if (!_cssParts.ContainsKey(location))
                _cssParts.Add(location, new List<CssReferenceMeta>());

            if (string.IsNullOrEmpty(part))
                return;

            _cssParts[location].Add(new CssReferenceMeta
            {
                ExcludeFromBundle = excludeFromBundle,
                Part = part
            });
        }
        /// <param name="excludeFromBundle">Recorded but INERT — bundling was dropped (design §8)</param>
        public virtual void AppendCssFileParts(ResourceLocation location, string part, bool excludeFromBundle = false)
        {
            if (!_cssParts.ContainsKey(location))
                _cssParts.Add(location, new List<CssReferenceMeta>());

            if (string.IsNullOrEmpty(part))
                return;
            
            _cssParts[location].Insert(0, new CssReferenceMeta
            {
                ExcludeFromBundle = excludeFromBundle,
                Part = part
            });
        }
        /// <summary>
        /// Generate all CSS parts as individual &lt;link&gt; elements, in registration order.
        /// </summary>
        /// <param name="urlHelper">URL helper</param>
        /// <param name="location">A location of the script element</param>
        /// <param name="bundleFiles">IGNORED — bundling was dropped (design §8)</param>
        /// <returns>Generated string</returns>
        public virtual string GenerateCssFiles(IUrlHelper urlHelper, ResourceLocation location, bool? bundleFiles = null)
        {
            if (!_cssParts.ContainsKey(location) || _cssParts[location] == null)
                return "";

            if (!_cssParts.Any())
                return "";

            //see GenerateScripts: individual tags plus a cache-busting version suffix.
            //NOTE: System.Web.Optimization's CssRewriteUrlTransform used to rewrite relative
            //url(...) references inside a bundled stylesheet, because the bundle was served from a
            //different path than the source file. With no bundle the stylesheet is served from its
            //own location, so relative urls resolve natively and no transform is needed.
            var result = new StringBuilder();
            foreach (var path in _cssParts[location].Select(x => x.Part).Distinct())
            {
                result.AppendFormat("<link href=\"{0}\" rel=\"stylesheet\" type=\"{1}\" />", GetAssetUrl(urlHelper, path), MimeTypes.TextCss);
                result.AppendLine();
            }
            return result.ToString();
        }


        public virtual void AddCanonicalUrlParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
                       
            _canonicalUrlParts.Add(part);
        }
        public virtual void AppendCanonicalUrlParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;
                       
            _canonicalUrlParts.Insert(0, part);
        }
        public virtual string GenerateCanonicalUrls()
        {
            var result = new StringBuilder();
            foreach (var canonicalUrl in _canonicalUrlParts)
            {
                result.AppendFormat("<link rel=\"canonical\" href=\"{0}\" />", canonicalUrl);
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }


        public virtual void AddHeadCustomParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _headCustomParts.Add(part);
        }
        public virtual void AppendHeadCustomParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _headCustomParts.Insert(0, part);
        }
        public virtual string GenerateHeadCustom()
        {
            //use only distinct rows
            var distinctParts = _headCustomParts.Distinct().ToList();
            if (!distinctParts.Any())
                return "";

            var result = new StringBuilder();
            foreach (var path in distinctParts)
            {
                result.Append(path);
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }

        
        public virtual void AddPageCssClassParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _pageCssClassParts.Add(part);
        }
        public virtual void AppendPageCssClassParts(string part)
        {
            if (string.IsNullOrEmpty(part))
                return;

            _pageCssClassParts.Insert(0, part);
        }
        public virtual string GeneratePageCssClasses()
        {
            string result = string.Join(" ", _pageCssClassParts.AsEnumerable().Reverse().ToArray());
            return result;
        }


        /// <summary>
        /// Specify "edit page" URL
        /// </summary>
        /// <param name="url">URL</param>
        public virtual void AddEditPageUrl(string url)
        {
            _editPageUrl = url;
        }
        /// <summary>
        /// Get "edit page" URL
        /// </summary>
        /// <returns>URL</returns>
        public virtual string GetEditPageUrl()
        {
            return _editPageUrl;
        }


        /// <summary>
        /// Specify system name of admin menu item that should be selected (expanded)
        /// </summary>
        /// <param name="systemName">System name</param>
        public virtual void SetActiveMenuItemSystemName(string systemName)
        {
            _activeAdminMenuSystemName = systemName;
        }
        /// <summary>
        /// Get system name of admin menu item that should be selected (expanded)
        /// </summary>
        /// <returns>System name</returns>
        public virtual string GetActiveMenuItemSystemName()
        {
            return _activeAdminMenuSystemName;
        }

        #endregion

        #region Nested classes

        private class ScriptReferenceMeta
        {
            /// <summary>
            /// INERT — retained so the registration API keeps its shape (design §8)
            /// </summary>
            public bool ExcludeFromBundle { get; set; }

            public bool IsAsync { get; set; }

            public string Part { get; set; }
        }

        private class CssReferenceMeta
        {
            /// <summary>
            /// INERT — retained so the registration API keeps its shape (design §8)
            /// </summary>
            public bool ExcludeFromBundle { get; set; }

            public string Part { get; set; }
        }
        #endregion
    }
}
