using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Core;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Product service interface
    /// </summary>
    public interface IProductService
    {
        Task<Product?> GetProductByIdAsync(int productId);
        Task<IList<Product>> GetProductsByIdsAsync(int[] productIds);
        Task<IPagedList<Product>> SearchProductsAsync(string keywords = "", int pageIndex = 0, int pageSize = int.MaxValue);
        Task<IPagedList<Product>> GetProductsByCategoryAsync(int categoryId, int pageIndex = 0, int pageSize = int.MaxValue);
        Task InsertProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(Product product);
    }

    /// <summary>
    /// Paged list interface
    /// </summary>
    public interface IPagedList<T> : IList<T>
    {
        int PageIndex { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }
}
