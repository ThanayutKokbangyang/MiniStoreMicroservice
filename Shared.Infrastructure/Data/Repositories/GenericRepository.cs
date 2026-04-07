using Dapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Shared.Common.DTOs.Common;
using Shared.Common.Interfaces.Repositories;
using Shared.Common.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Data.Repositories
{
    public abstract class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        // ============================================================
        // Generic Repository Implementation with Dapper
        // Design Pattern: Repository Pattern + Template Method
        // ป้องกัน SQL Injection: ใช้ Parameterized Queries เท่านั้น
        // ============================================================
        protected readonly IDbConnectionFactory _connectionFactory;
        protected readonly ILogger _logger;

        protected abstract string TableName { get; }

        protected GenericRepository(IDbConnectionFactory connectionFactory, ILogger logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            //Parameterized query - ป้องกัน SQL Injection
            var sql = $"SELECT * FROM {TableName} WHERE Id = @Id AND IsActive = 1";
            return await connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = $"SELECT * FROM {TableName} WHERE IsActive = 1 ORDER BY CreateAt DESC";
            return await connection.QueryAsync<T>(sql);
        }

        public virtual async Task<PaginatedResponse<T>> GetPaginatedAsync(PaginationRequest request)
        {
            using var connection = _connectionFactory.CreateConnection();

            //WhiteList สำหรับ sort columns - ป้องกัน SQL Injection ผ่าน ORDER BY
            var allowedSortColumns = GetAllowedSortColumns();
            var sortBy = allowedSortColumns.Contains(request.SortBy ?? "CreateAt", StringComparer.OrdinalIgnoreCase)
                ? request.SortBy! : "CreateAt";
            var sortDir = request.SortDirection?.ToUpper() == "ASC" ? "ASC" : "DESC";

            var offset = (request.PageNumber - 1) * request.PageSize;

            var countSql = $"SELECT COUNT(*) FROM {TableName} WHERE IsActive = 1";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

            var sql = $"SELECT * FROM {TableName} WHERE IsActive = 1 ORDER BY {sortBy} {sortDir} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var items = await connection.QueryAsync<T>(sql, new { Offset = offset, PageSize = request.PageSize });

            return new PaginatedResponse<T>
            {
                Items = items.ToList(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public virtual async Task<bool> DeleteAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            //Soft Delete
            var sql = $"UPDATE {TableName} SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = @Id";
            var affected = await connection.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        public virtual async Task<bool> HardDeleteAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = $"DELETE FROM {TableName} WHERE Id = @Id";
            var affected = await connection.ExecuteAsync(sql, new { Id = id });
            return affected > 0;
        }

        public abstract Task<int> CreateAsync(T entity);
        
        public abstract Task<bool> UpdateAsync(T entity);

        protected abstract IEnumerable<string> GetAllowedSortColumns();

    }
}
