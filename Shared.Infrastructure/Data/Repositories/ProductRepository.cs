using Dapper;
using Microsoft.Extensions.Logging;
using Shared.Common.DTOs.Common;
using Shared.Common.Interfaces.Repositories;
using Shared.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Data.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        protected override string TableName => "Products";

        public ProductRepository(IDbConnectionFactory connectionFactory, ILogger<ProductRepository> logger) : base(connectionFactory, logger) {}

        protected override IEnumerable<string> GetAllowedSortColumns()
            => new[] { "Id", "Name", "Price", "StockQuantity", "Category", "CreatedAt" };

        public override async Task<int> CreateAsync(Product entity)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Products (Name, Description, Price, StockQuantity, Category, SKU, CreatedBy)
                        VALUES (@Name, @Description, @Price, @StockQuantity, @Category, @SKU, @CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() as int);";
            return await connection.ExecuteScalarAsync<int>(sql, entity);
        }

        public override async Task<bool> UpdateAsync(Product entity)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"UPDATE Products SET
                        Name = @Name, Description = @Description, Price = @Price,
                        StockQuantity = @StockQuantity, Category = @Category,
                        UpdatedAt = GETUTCDATE(), UpdatedBy = @UpdateBy
                        WHERE Id = @Id AND IsActive = 1";
            return await connection.ExecuteAsync(sql, entity) > 0;
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<Product>("SELECT * FROM Products WHERE Category = @Category AND IsActive = 1", new { Category = category });
        }

        public async Task<Product?> GetBySkuAsync(string sku)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Products WHERE SKU = @SKU AND IsActive = 1", new { SKU = sku });
        }

        public async Task<bool> UpdateStockAsync(int productId, int quantity)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteAsync("UPDATE Products SET StockQuantity = StockQuantity + @Quantity, UpdatedAt = GETUTCDATE() WHERE Id = @Id AND IsActive = 1",
                new { Id = productId, Quantity = quantity }) > 0;
        }

        public async Task<PaginatedResponse<Product>> SearchAsync(string searchTerm, PaginationRequest pagination)
        {
            using var connection = _connectionFactory.CreateConnection();
            var offset = (pagination.PageNumber - 1) * pagination.PageSize;

            //ใช้ LIKE กับ parameterized query ป้องกัน SQL Injection
            var countSql = @"SELECT COUNT(*) FROM Products WHERE IsActive = 1 AND (Name LIKE @Search Or Description Like @Search OR Category LIKE @Search)";

            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { Search = $"%{searchTerm}%" });

            var sql = @"SELECT * FROM Products WHERE IsActive = 1 AND (Name Like @Search OR Description LIKE @Search OR Category LIKE @Search) 
                        ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = await connection.QueryAsync<Product>(sql, new { Search = $"%{searchTerm}%", Offset = offset, PageSize = pagination.PageSize });

            return new PaginatedResponse<Product>
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize
            };
        }

    }
}
