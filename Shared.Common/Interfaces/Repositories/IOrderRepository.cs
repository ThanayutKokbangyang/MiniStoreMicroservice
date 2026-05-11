using Shared.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Interfaces.Repositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
        Task<Order?> GetByOrderNumberAsync(string orderNumber);
        Task<bool> UpdateStatusAsync(int orderId, string status);
        Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);
        Task<int> CreateOrderItemAsync(OrderItem item);
    }
}
