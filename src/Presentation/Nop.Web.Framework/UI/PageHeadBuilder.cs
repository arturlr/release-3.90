using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.Seo;
using Nop.Services.Seo;

namespace Nop.Web.Framework.UI
{
    public partial class PageHeadBuilder : IPageHeadBuilder
    {
        private readonly SeoSettings _seoSettings;
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

        public PageHeadBuilder(SeoSettings seoSettings)
        {
            this._seoSettings = seoSettings;
            this._titleParts = new List<string>();
            this._metaDescriptionParts = new List<string>();
            this._metaKeywordParts = new List<string>();
            this._scriptParts = new Dictionary<ResourceLocation, List<ScriptReferenceMeta>>();
            this._cssParts = new Dictionary<ResourceLocation, List<CssReferenceMeta>>();
            this._canonicalUrlParts = new List<string>();
            this._headCustomParts = new List<string>();
            this._pageCssClassParts = new List<string>();
        }

        public virtual void AddTitleParts(string part) { if (!string.IsNullOrEmpty(part)) _titleParts.Add(part); }
        public virtual void AppendTitleParts(string part) { if (!string.IsNullOrEmpty(part)) _titleParts.Insert(0, part); }
        public virtual string GenerateTitle(bool addDefaultTitle)
        {
            string result = "";
            var specificTitle = string.Join(_seoSettings.PageTitleSeparator, _titleParts.AsEnumerable().Reverse().ToArray());
            if (!String.IsNullOrEmpty(specificTitle))
            {
                if (addDefaultTitle)
                {
                    switch (_seoSettings.PageTitleSeoAdjustment)
                    {
                        case PageTitleSeoAdjustment.PagenameAfterStorename:
                            result = string.Join(_seoSettings.PageTitleSeparator, _seoSettings.DefaultTitle, specificTitle);
                            break;
                        default:
                            result = string.Join(_seoSettings.PageTitleSeparator, specificTitle, _seoSettings.DefaultTitle);
                            break;
                    }
                }
                else
                    result = specificTitle;
            }
            else
                result = _seoSettings.DefaultTitle;
            return result;
        }

        public virtual void AddMetaDescriptionParts(string part) { if (!string.IsNullOrEmpty(part)) _metaDescriptionParts.Add(part); }
        public virtual void AppendMetaDescriptionParts(string part) { if (!string.IsNullOrEmpty(part)) _metaDescriptionParts.Insert(0, part); }
        public virtual string GenerateMetaDescription()
        {
            var metaDescription = string.Join(", ", _metaDescriptionParts.AsEnumerable().Reverse().ToArray());
            return !String.IsNullOrEmpty(metaDescription) ? metaDescription : _seoSettings.DefaultMetaDescription;
        }

        public virtual void AddMetaKeywordParts(string part) { if (!string.IsNullOrEmpty(part)) _metaKeywordParts.Add(part); }
        public virtual void AppendMetaKeywordParts(string part) { if (!string.IsNullOrEmpty(part)) _metaKeywordParts.Insert(0, part); }
        public virtual string GenerateMetaKeywords()
        {
            var metaKeyword = string.Join(", ", _metaKeywordParts.AsEnumerable().Reverse().ToArray());
            return !String.IsNullOrEmpty(metaKeyword) ? metaKeyword : _seoSettings.DefaultMetaKeywords;
        }

        public virtual void AddScriptParts(ResourceLocation location, string part, bool excludeFromBundle, bool isAsync)
        {
            if (!_scriptParts.ContainsKey(location)) _scriptParts.Add(location, new List<ScriptReferenceMeta>());
            if (string.IsNullOrEmpty(part)) return;
            _scriptParts[location].Add(new ScriptReferenceMeta { ExcludeFromBundle = excludeFromBundle, IsAsync = isAsync, Part = part });
        }
        public virtual void AppendScriptParts(ResourceLocation location, string part, bool excludeFromBundle, bool isAsync)
        {
            if (!_scriptParts.ContainsKey(location)) _scriptParts.Add(location, new List<ScriptReferenceMeta>());
            if (string.IsNullOrEmpty(part)) return;
            _scriptParts[location].Insert(0, new ScriptReferenceMeta { ExcludeFromBundle = excludeFromBundle, IsAsync = isAsync, Part = part });
        }
        public virtual string GenerateScripts(ResourceLocation location, bool? bundleFiles = null)
        {
            if (!_scriptParts.ContainsKey(location) || _scriptParts[location] == null) return "";
            if (!_scriptParts.Any()) return "";
            var result = new StringBuilder();
            foreach (var item in _scriptParts[location].Select(x => new { x.Part, x.IsAsync }).Distinct())
            {
                var resolvedPath = item.Part.Replace("~/", "/");
                result.AppendFormat("<script {2}src=\"{0}\" type=\"{1}\"></script>", resolvedPath, MimeTypes.TextJavascript, item.IsAsync ? "async " : "");
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }

        public virtual void AddCssFileParts(ResourceLocation location, string part, bool excludeFromBundle = false)
        {
            if (!_cssParts.ContainsKey(location)) _cssParts.Add(location, new List<CssReferenceMeta>());
            if (string.IsNullOrEmpty(part)) return;
            _cssParts[location].Add(new CssReferenceMeta { ExcludeFromBundle = excludeFromBundle, Part = part });
        }
        public virtual void AppendCssFileParts(ResourceLocation location, string part, bool excludeFromBundle = false)
        {
            if (!_cssParts.ContainsKey(location)) _cssParts.Add(location, new List<CssReferenceMeta>());
            if (string.IsNullOrEmpty(part)) return;
            _cssParts[location].Insert(0, new CssReferenceMeta { ExcludeFromBundle = excludeFromBundle, Part = part });
        }
        public virtual string GenerateCssFiles(ResourceLocation location, bool? bundleFiles = null)
        {
            if (!_cssParts.ContainsKey(location) || _cssParts[location] == null) return "";
            if (!_cssParts.Any()) return "";
            var result = new StringBuilder();
            foreach (var path in _cssParts[location].Select(x => x.Part).Distinct())
            {
                var resolvedPath = path.Replace("~/", "/");
                result.AppendFormat("<link href=\"{0}\" rel=\"stylesheet\" type=\"{1}\" />", resolvedPath, MimeTypes.TextCss);
                result.AppendLine();
            }
            return result.ToString();
        }

        public virtual void AddCanonicalUrlParts(string part) { if (!string.IsNullOrEmpty(part)) _canonicalUrlParts.Add(part); }
        public virtual void AppendCanonicalUrlParts(string part) { if (!string.IsNullOrEmpty(part)) _canonicalUrlParts.Insert(0, part); }
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

        public virtual void AddHeadCustomParts(string part) { if (!string.IsNullOrEmpty(part)) _headCustomParts.Add(part); }
        public virtual void AppendHeadCustomParts(string part) { if (!string.IsNullOrEmpty(part)) _headCustomParts.Insert(0, part); }
        public virtual string GenerateHeadCustom()
        {
            var distinctParts = _headCustomParts.Distinct().ToList();
            if (!distinctParts.Any()) return "";
            var result = new StringBuilder();
            foreach (var path in distinctParts) { result.Append(path); result.Append(Environment.NewLine); }
            return result.ToString();
        }

        public virtual void AddPageCssClassParts(string part) { if (!string.IsNullOrEmpty(part)) _pageCssClassParts.Add(part); }
        public virtual void AppendPageCssClassParts(string part) { if (!string.IsNullOrEmpty(part)) _pageCssClassParts.Insert(0, part); }
        public virtual string GeneratePageCssClasses() { return string.Join(" ", _pageCssClassParts.AsEnumerable().Reverse().ToArray()); }

        public virtual void AddEditPageUrl(string url) { _editPageUrl = url; }
        public virtual string GetEditPageUrl() { return _editPageUrl; }
        public virtual void SetActiveMenuItemSystemName(string systemName) { _activeAdminMenuSystemName = systemName; }
        public virtual string GetActiveMenuItemSystemName() { return _activeAdminMenuSystemName; }

        private class ScriptReferenceMeta { public bool ExcludeFromBundle { get; set; } public bool IsAsync { get; set; } public string Part { get; set; } }
        private class CssReferenceMeta { public bool ExcludeFromBundle { get; set; } public string Part { get; set; } }
    }
}
