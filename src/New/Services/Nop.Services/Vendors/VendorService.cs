using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Vendors;
using Nop.Services.Events;

namespace Nop.Services.Vendors;

public class VendorService : IVendorService
{
    private readonly IRepository<Vendor> _vendorRepository;
    private readonly IRepository<VendorNote> _vendorNoteRepository;
    private readonly IEventPublisher _eventPublisher;

    public VendorService(
        IRepository<Vendor> vendorRepository,
        IRepository<VendorNote> vendorNoteRepository,
        IEventPublisher eventPublisher)
    {
        _vendorRepository = vendorRepository;
        _vendorNoteRepository = vendorNoteRepository;
        _eventPublisher = eventPublisher;
    }

    public Task<Vendor?> GetVendorByIdAsync(int vendorId)
    {
        return Task.FromResult(vendorId == 0 ? null : _vendorRepository.GetById(vendorId));
    }

    public async Task DeleteVendorAsync(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        vendor.Deleted = true;
        _vendorRepository.Update(vendor);

        await _eventPublisher.EntityDeletedAsync(vendor);
    }

    public Task<IPagedList<Vendor>> GetAllVendorsAsync(
        string name = "",
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        bool showHidden = false)
    {
        var query = _vendorRepository.Table.Where(v => !v.Deleted);

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(v => v.Name != null && v.Name.Contains(name));

        if (!showHidden)
            query = query.Where(v => v.Active);

        query = query.OrderBy(v => v.DisplayOrder).ThenBy(v => v.Name);

        IPagedList<Vendor> result = new PagedList<Vendor>(query, pageIndex, pageSize);
        return Task.FromResult(result);
    }

    public async Task InsertVendorAsync(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        _vendorRepository.Insert(vendor);

        await _eventPublisher.EntityInsertedAsync(vendor);
    }

    public async Task UpdateVendorAsync(Vendor vendor)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        _vendorRepository.Update(vendor);

        await _eventPublisher.EntityUpdatedAsync(vendor);
    }

    public Task<VendorNote?> GetVendorNoteByIdAsync(int vendorNoteId)
    {
        return Task.FromResult(vendorNoteId == 0 ? null : _vendorNoteRepository.GetById(vendorNoteId));
    }

    public async Task DeleteVendorNoteAsync(VendorNote vendorNote)
    {
        ArgumentNullException.ThrowIfNull(vendorNote);

        _vendorNoteRepository.Delete(vendorNote);

        await _eventPublisher.EntityDeletedAsync(vendorNote);
    }
}
