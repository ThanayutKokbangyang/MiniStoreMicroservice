using Shared.Common.DTOs.Common;
using Shared.Common.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Interfaces.Services
{
    public interface IProductService
    {
        Task<ApiResponse<ProductResponse>> GetByIdAsync(int id);
        Task<ApiResponse<PaginatedResponse<ProductResponse>>> GetAllAsync(PaginationRequest request);
        Task<ApiResponse<ProductResponse>> CreateAsync(CreateProductRequest request, string userId);
        Task<ApiResponse<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(int id);
        Task<ApiResponse<PaginatedResponse<ProductResponse>>> SearchAsync(string term, PaginationRequest request);

    }
}
