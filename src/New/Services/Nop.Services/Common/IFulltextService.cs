namespace Nop.Services.Common;

public interface IFulltextService
{
    Task<bool> IsFullTextSupportedAsync();
    Task EnableFullTextAsync();
    Task DisableFullTextAsync();
}
