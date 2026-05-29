using System;
using System.Linq;

namespace Nop.Web.Framework.Localization
{
    public static class LocalizedUrlExtenstions
    {
        private static int _seoCodeLength = 2;

        public static bool IsLocalizedUrl(this string url, string applicationPath, bool isRawPath)
        {
            if (string.IsNullOrEmpty(url)) return false;
            var path = GetPathFromUrl(url, applicationPath);
            if (string.IsNullOrEmpty(path) || path.Length < _seoCodeLength + 1) return false;
            if (path[0] != '/') return false;
            var seoCode = path.Substring(1, _seoCodeLength);
            return path.Length == _seoCodeLength + 1 || path[_seoCodeLength + 1] == '/';
        }

        public static string GetLanguageSeoCodeFromUrl(this string url, string applicationPath, bool isRawPath)
        {
            if (!IsLocalizedUrl(url, applicationPath, isRawPath)) return string.Empty;
            var path = GetPathFromUrl(url, applicationPath);
            return path.Substring(1, _seoCodeLength);
        }

        public static string RemoveLanguageSeoCodeFromRawUrl(this string url, string applicationPath)
        {
            if (string.IsNullOrEmpty(url)) return url;
            var path = GetPathFromUrl(url, applicationPath);
            if (path.Length <= _seoCodeLength + 1) return applicationPath ?? "/";
            return path.Substring(_seoCodeLength + 1);
        }

        public static string AddLanguageSeoCodeToRawUrl(this string url, string applicationPath, string seoCode)
        {
            return $"/{seoCode}{url}";
        }

        private static string GetPathFromUrl(string url, string applicationPath)
        {
            if (!string.IsNullOrEmpty(applicationPath) && applicationPath != "/" && url.StartsWith(applicationPath, StringComparison.OrdinalIgnoreCase))
                url = url.Substring(applicationPath.Length);
            return url;
        }
    }
}
