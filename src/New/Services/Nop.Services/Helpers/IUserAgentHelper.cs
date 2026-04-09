namespace Nop.Services.Helpers;

/// <summary>
/// User agent helper — detects search engine crawlers
/// </summary>
public interface IUserAgentHelper
{
    bool IsSearchEngine();
}
