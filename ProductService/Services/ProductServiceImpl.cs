using Shared.Common.DTOs.Common;
using Shared.Common.DTOs.Product;
using Shared.Common.Exceptions;
using Shared.Common.Interfaces.Infrastructure;
using Shared.Common.Interfaces.Repositories;
using Shared.Common.Interfaces.Services;
using Shared.Common.Models;
using Shared.Infrastructure.Logging;

namespace ProductService.Services
{
    public class ProductServiceImpl : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly ICacheService _cache;
        private readonly IAuditLogService _auditLog;
        private readonly ILogger<ProductServiceImpl> _logger;

        public ProductServiceImpl(
        IProductRepository productRepo, ICacheService cache,
        IAuditLogService auditLog, ILogger<ProductServiceImpl> logger)
        {
            _productRepo = productRepo;
            _cache = cache;
            _auditLog = auditLog;
            _logger = logger;
        }

        public async Task<ApiResponse<ProductResponse>> GetByIdAsync(int id)
        {
            // Cache-Aside Pattern
            var cacheKey = $"product:{id}";
            var cached = await _cache.GetAsync<ProductResponse>(cacheKey);
            if (cached != null)
                return ApiResponse<ProductResponse>.SuccessResponse(cached);

            var product = await _productRepo.GetByIdAsync(id)
            ?? throw new NotFoundException("Product", id);

            var response = MapToResponse(product);
            await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10));
            return ApiResponse<ProductResponse>.SuccessResponse(response);
        }

        public async Task<ApiResponse<PaginatedResponse<ProductResponse>>> GetAllAsync(PaginationRequest request)
        {
            var result = await _productRepo.GetPaginatedAsync(request);
            var mapped = new PaginatedResponse<ProductResponse>
            {
                Items = result.Items.Select(MapToResponse).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
            return ApiResponse<PaginatedResponse<ProductResponse>>.SuccessResponse(mapped);
        }

        public async Task<ApiResponse<ProductResponse>> CreateAsync(CreateProductRequest request, string userId)
        {
            // Check SKU uniqueness
            if (await _productRepo.GetBySkuAsync(request.SKU) != null)
                throw new ConflictException($"SKU '{request.SKU}' already exists.");

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                Category = request.Category,
                SKU = request.SKU,
                CreatedBy = userId
            };

            product.Id = await _productRepo.CreateAsync(product);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                UserId = int.TryParse(userId, out var uid) ? uid : null,
                Action = "Create",
                EntityType = "Product",
                EntityId = product.Id.ToString(),
                NewValues = request,
                ServiceName = "ProductService"
            });

            var response = MapToResponse(product);
            return ApiResponse<ProductResponse>.SuccessResponse(response, "Product created successfully");
        }

        public async Task<ApiResponse<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request, string userId)
        {
            var product = await _productRepo.GetByIdAsync(id) ?? throw new NotFoundException("Product", id);

            var oldValues = MapToResponse(product);

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.Category = request.Category;
            product.UpdatedBy = userId;

            await _productRepo.UpdateAsync(product);
            await _cache.RemoveAsync($"product:{id}");

            await _auditLog.LogAsync(new AuditLogEntry
            {
                Action = "Update",
                EntityType = "Product",
                EntityId = id.ToString(),
                OldValues = oldValues,
                NewValues = request,
                ServiceName = "ProductService"
            });

            return ApiResponse<ProductResponse>.SuccessResponse(MapToResponse(product), "Product updated");
        }
        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var product = await _productRepo.GetByIdAsync(id) ?? throw new NotFoundException("Product", id);

            await _productRepo.DeleteAsync(id);
            await _cache.RemoveAsync($"product:{id}");

            await _auditLog.LogAsync(new AuditLogEntry
            {
                Action = "Delete",
                EntityType = "Product",
                EntityId = id.ToString(),
                ServiceName = "ProductService"
            });

            return ApiResponse<bool>.SuccessResponse(true, "Product deleted");
        }

        public async Task<ApiResponse<PaginatedResponse<ProductResponse>>> SearchAsync(string term, PaginationRequest request)
        {
            var result = await _productRepo.SearchAsync(term, request);
            var mapped = new PaginatedResponse<ProductResponse>
            {
                Items = result.Items.Select(MapToResponse).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };

            return ApiResponse<PaginatedResponse<ProductResponse>>.SuccessResponse(mapped);
        }

        private static ProductResponse MapToResponse(Product p) => new(p.Id, p.Name, p.Description, p.Price, p.StockQuantity, p.Category, p.SKU, p.CreatedAt);

    }
}
