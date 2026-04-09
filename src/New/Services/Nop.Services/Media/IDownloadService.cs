using Nop.Core.Domain.Media;

namespace Nop.Services.Media;

public interface IDownloadService
{
    Task<Download?> GetDownloadByIdAsync(int downloadId);

    Task<Download?> GetDownloadByGuidAsync(Guid downloadGuid);

    Task DeleteDownloadAsync(Download download);

    Task InsertDownloadAsync(Download download);

    Task UpdateDownloadAsync(Download download);
}
