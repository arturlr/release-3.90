using System;
using Nop.Core.Domain.Localization;

namespace Nop.Web.Framework.Localization
{
    public static class LocalizedUrlExtenstions
    {
        private static int _seoCodeLength = 2;

        private static bool IsVirtualDirectory(this string applicationPath)
        {
            if (string.IsNullOrEmpty(applicationPath))
                throw new ArgumentException("Application path is not specified");
            return applicationPath != "/";
        }

        public static string RemoveApplicationPathFromRawUrl(this string rawUrl, string applicationPath)
        {
            if (string.IsNullOrEmpty(applicationPath))
                throw new ArgumentException("Application path is not specified");
            if (rawUrl.Length == applicationPath.Length)
                return "/";
            var result = rawUrl.Substring(applicationPath.Length);
            if (!result.StartsWith("/"))
                result = "/" + result;
            return result;
        }

        public static string GetLanguageSeoCodeFromUrl(this string url, string applicationPath, bool isRawPath)
        {
            if (isRawPath)
            {
                if (applicationPath.IsVirtualDirectory())
                    url = url.RemoveApplicationPathFromRawUrl(applicationPath);
                return url.Substring(1, _seoCodeLength);
            }
            return url.Substring(2, _seoCodeLength);
        }

        public static bool IsLocalizedUrl(this string url, string applicationPath, bool isRawPath)
        {
            if (string.IsNullOrEmpty(url))
                return false;
            if (isRawPath)
            {
                if (applicationPath.IsVirtualDirectory())
                    url = url.RemoveApplicationPathFromRawUrl(applicationPath);
                int length = url.Length;
                if (length < 1 + _seoCodeLength)
                    return false;
                if (length == 1 + _seoCodeLength)
                    return true;
                return (length > 1 + _seoCodeLength) && (url[1 + _seoCodeLength] == '/');
            }
            else
            {
                int length = url.Length;
                if (length < 2 + _seoCodeLength)
                    return false;
                if (length == 2 + _seoCodeLength)
                    return true;
                return (length > 2 + _seoCodeLength) && (url[2 + _seoCodeLength] == '/');
            }
        }

        public static string RemoveLanguageSeoCodeFromRawUrl(this string url, string applicationPath)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            string result = null;
            if (applicationPath.IsVirtualDirectory())
                url = url.RemoveApplicationPathFromRawUrl(applicationPath);

            int length = url.Length;
            if (length < _seoCodeLength + 1)
                result = url;
            else if (length == 1 + _seoCodeLength)
                result = url.Substring(0, 1);
            else
                result = url.Substring(_seoCodeLength + 1);

            if (applicationPath.IsVirtualDirectory())
                result = applicationPath + result;
            return result;
        }

        public static string AddLanguageSeoCodeToRawUrl(this string url, string applicationPath, Language language)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            int startIndex = 0;
            if (applicationPath.IsVirtualDirectory())
                startIndex = applicationPath.Length;

            url = url.Insert(startIndex, language.UniqueSeoCode);
            url = url.Insert(startIndex, "/");
            return url;
        }
    }
}
