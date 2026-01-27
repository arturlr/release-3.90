using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Vendors;
using Nop.Data;

namespace Nop.Services.Vendors
{
    public class VendorService : IVendorService
    {
        private readonly IRepository<Vendor> _vendorRepository;

        public VendorService(IRepository<Vendor> vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }

        public virtual async Task<Vendor> GetVendorByIdAsync(int vendorId)
        {
            if (vendorId == 0)
                return null;

            return await _vendorRepository.GetByIdAsync(vendorId);
        }

        public virtual async Task<IList<Vendor>> GetAllVendorsAsync()
        {
            var query = _vendorRepository.Table
                .Where(v => v.Active && !v.Deleted)
                .OrderBy(v => v.DisplayOrder)
                .ThenBy(v => v.Name);

            return await query.ToListAsync();
        }

        public virtual async Task InsertVendorAsync(Vendor vendor)
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            await _vendorRepository.InsertAsync(vendor);
        }

        public virtual async Task UpdateVendorAsync(Vendor vendor)
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            await _vendorRepository.UpdateAsync(vendor);
        }

        public virtual async Task DeleteVendorAsync(Vendor vendor)
        {
            if (vendor == null)
                throw new ArgumentNullException(nameof(vendor));

            vendor.Deleted = true;
            await _vendorRepository.UpdateAsync(vendor);
        }
    }
}
