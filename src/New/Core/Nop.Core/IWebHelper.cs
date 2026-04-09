namespace Nop.Core;

/// <summary>
/// Web helper — provides HTTP request utilities (URL, IP, SSL, query string)
/// </summary>
public interface IWebHelper
{
    string? GetUrlReferrer();
    string GetCurrentIpAddress();
    string GetThisPageUrl(bool includeQueryString);
    string GetThisPageUrl(bool includeQueryString, bool useSsl);
    bool IsCurrentConnectionSecured();
    string GetStoreHost(bool useSsl);
    string GetStoreLocation();
    string GetStoreLocation(bool useSsl);
    bool IsStaticResource();
    string ModifyQueryString(string url, string queryStringModification, string? anchor);
    string RemoveQueryString(string url, string queryString);
    T? QueryString<T>(string name);
    bool IsRequestBeingRedirected { get; }
    bool IsPostBeingDone { get; set; }
}
