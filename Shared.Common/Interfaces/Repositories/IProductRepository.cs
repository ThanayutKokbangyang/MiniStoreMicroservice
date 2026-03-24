using Shared.Common.DTOs.Common;
using Shared.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Interfaces.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<IEnumerable<Product>> GetByCategoryAsync(string category);
        Task<Product?> GetBySkuAsync(string sku);
        Task<bool> UpdateStockAsync(int productId, int quantity);
        Task<PaginatedResponse<Product>> SearchAsync(string searchTerm, PaginationRequest pagination);

    }
}
