using Nop.Core.Data;
using Nop.Core.Domain.Media;
using Nop.Services.Events;

namespace Nop.Services.Media;

public class DownloadService : IDownloadService
{
    private readonly IRepository<Download> _downloadRepository;
    private readonly IEventPublisher _eventPublisher;

    public DownloadService(
        IRepository<Download> downloadRepository,
        IEventPublisher eventPublisher)
    {
        _downloadRepository = downloadRepository;
        _eventPublisher = eventPublisher;
    }

    public Task<Download?> GetDownloadByIdAsync(int downloadId)
    {
        return Task.FromResult(downloadId == 0 ? null : _downloadRepository.GetById(downloadId));
    }

    public Task<Download?> GetDownloadByGuidAsync(Guid downloadGuid)
    {
        if (downloadGuid == Guid.Empty)
            return Task.FromResult<Download?>(null);

        var download = _downloadRepository.Table
            .FirstOrDefault(d => d.DownloadGuid == downloadGuid);

        return Task.FromResult(download);
    }

    public async Task DeleteDownloadAsync(Download download)
    {
        ArgumentNullException.ThrowIfNull(download);

        _downloadRepository.Delete(download);

        await _eventPublisher.EntityDeletedAsync(download);
    }

    public async Task InsertDownloadAsync(Download download)
    {
        ArgumentNullException.ThrowIfNull(download);

        _downloadRepository.Insert(download);

        await _eventPublisher.EntityInsertedAsync(download);
    }

    public async Task UpdateDownloadAsync(Download download)
    {
        ArgumentNullException.ThrowIfNull(download);

        _downloadRepository.Update(download);

        await _eventPublisher.EntityUpdatedAsync(download);
    }
}
