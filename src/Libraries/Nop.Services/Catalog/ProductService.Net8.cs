using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Microsoft.EntityFrameworkCore;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Product service implementation
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IRepository<Product> _productRepository;

        public ProductService(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        /// <summary>
        /// Gets a product by ID
        /// </summary>
        public virtual async Task<Product?> GetProductByIdAsync(int productId)
        {
            if (productId == 0)
                return null;

            return await _productRepository.GetByIdAsync(productId);
        }

        /// <summary>
        /// Gets products by IDs
        /// </summary>
        public virtual async Task<IList<Product>> GetProductsByIdsAsync(int[] productIds)
        {
            if (productIds == null || productIds.Length == 0)
                return new List<Product>();

            var query = _productRepository.Table.Where(p => productIds.Contains(p.Id));
            return await query.ToListAsync();
        }

        /// <summary>
        /// Search products
        /// </summary>
        public virtual async Task<IPagedList<Product>> SearchProductsAsync(
            string keywords = "",
            int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            var query = _productRepository.Table;

            // Filter by keywords
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                query = query.Where(p => p.Name.Contains(keywords));
            }

            // Filter published only
            query = query.Where(p => p.Published);

            // Order by name
            query = query.OrderBy(p => p.Name);

            // Paging
            var products = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await query.CountAsync();

            return new PagedList<Product>(products, pageIndex, pageSize, totalCount);
        }

        /// <summary>
        /// Gets products by category
        /// </summary>
        public virtual async Task<IPagedList<Product>> GetProductsByCategoryAsync(
            int categoryId,
            int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            // TODO: Join with ProductCategory table
            var query = _productRepository.Table.Where(p => p.Published);
            
            var products = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await query.CountAsync();

            return new PagedList<Product>(products, pageIndex, pageSize, totalCount);
        }

        /// <summary>
        /// Inserts a product
        /// </summary>
        public virtual async Task InsertProductAsync(Product product)
        {
            await _productRepository.InsertAsync(product);
        }

        /// <summary>
        /// Updates a product
        /// </summary>
        public virtual async Task UpdateProductAsync(Product product)
        {
            await _productRepository.UpdateAsync(product);
        }

        /// <summary>
        /// Deletes a product
        /// </summary>
        public virtual async Task DeleteProductAsync(Product product)
        {
            await _productRepository.DeleteAsync(product);
        }
    }

    /// <summary>
    /// Paged list implementation
    /// </summary>
    public class PagedList<T> : List<T>, IPagedList<T>
    {
        public PagedList(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
        {
            AddRange(source);
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        public int PageIndex { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageIndex > 0;
        public bool HasNextPage => PageIndex + 1 < TotalPages;
    }
}
