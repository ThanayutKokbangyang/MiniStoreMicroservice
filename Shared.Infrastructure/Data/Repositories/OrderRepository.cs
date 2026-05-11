using Dapper;
using Microsoft.Extensions.Logging;
using Shared.Common.Interfaces.Repositories;
using Shared.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Infrastructure.Data.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        protected override string TableName => "Orders";

        public OrderRepository(IDbConnectionFactory connectionFactory, ILogger<OrderRepository> logger): base(connectionFactory, logger) { }

        protected override IEnumerable<string> GetAllowedSortColumns() 
            => new[] { "Id", "OrderNumber", "TotalAmount", "Status", "CreatedAt" };

        public override async Task<int> CreateAsync(Order entity)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Orders (UserId, OrderNumber, TotalAmount, Status, ShippingAddress, CreatedBy)
                        VALUES (@UserId, @OrderNumber, @TotalAmount, @Status, @ShippingAddress, @CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() as int);";
            return await connection.ExecuteScalarAsync<int>(sql, entity);
        }

        public override async Task<bool> UpdateAsync(Order entity)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"UPDATE Orders SET Status = @Status, ShippingAddress = @ShippingAddress,
                        UpdatedAt = GETUTCDATE(), UpdatedBy = @UpdatedBy
                        WHERE Id = @Id AND IsActive = 1";
            return await connection.ExecuteAsync(sql, entity) > 0;
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<Order>(
                "SELECT * FROM Orders WHERE UserId = @UserId AND IsActive = 1 ORDER BY CreatedAt DESC",
                new { UserId = userId });
        }

        public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Order>(
                "SELECT * FROM Orders WHERE OrderNumber = @OrderNumber AND IsActive = 1",
                new { OrderNumber = orderNumber });
        }

        public async Task<bool> UpdateStatusAsync(int orderId, string status)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteAsync(
                "UPDATE Orders SET Status = @Status, UpdatedAt = GETUTCDATE() WHERE Id = @Id AND IsActive = 1",
                new { Id = orderId, Status = status }) > 0;
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<OrderItem>(
                "SELECT * FROM OrderItems WHERE OrderId = @OrderId AND IsActive = 1",
                new { OrderId = orderId });

        }

        public async Task<int> CreateOrderItemAsync(OrderItem item)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO OrderItems(OrderId, ProductId, Quantity, UnitPrice, TotalPrice, CreatedBy)
                        VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @TotalPrice, @CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() as int);";
            return await connection.ExecuteScalarAsync<int>(sql, item);
        }
    }
}
