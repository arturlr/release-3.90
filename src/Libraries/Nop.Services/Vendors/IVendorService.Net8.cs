using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Vendors
{
    public interface IVendorService
    {
        Task<Vendor> GetVendorByIdAsync(int vendorId);
        Task<IList<Vendor>> GetAllVendorsAsync();
        Task InsertVendorAsync(Vendor vendor);
        Task UpdateVendorAsync(Vendor vendor);
        Task DeleteVendorAsync(Vendor vendor);
    }
}
