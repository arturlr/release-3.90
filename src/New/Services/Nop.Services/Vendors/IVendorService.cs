using Nop.Core;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Vendors;

public interface IVendorService
{
    Task<Vendor?> GetVendorByIdAsync(int vendorId);

    Task DeleteVendorAsync(Vendor vendor);

    Task<IPagedList<Vendor>> GetAllVendorsAsync(
        string name = "",
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        bool showHidden = false);

    Task InsertVendorAsync(Vendor vendor);

    Task UpdateVendorAsync(Vendor vendor);

    Task<VendorNote?> GetVendorNoteByIdAsync(int vendorNoteId);

    Task DeleteVendorNoteAsync(VendorNote vendorNote);
}
