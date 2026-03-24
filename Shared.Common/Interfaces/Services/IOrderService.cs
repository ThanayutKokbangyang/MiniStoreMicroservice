using Shared.Common.DTOs.Common;
using Shared.Common.DTOs.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.Interfaces.Services
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderResponse>> GetByIdAsync(int id, int userId);
        Task<ApiResponse<PaginatedResponse<OrderResponse>>> GetByUserIdAsync(int userId, PaginationRequest request);
        Task<ApiResponse<OrderResponse>> CreateAsync(CreateOrderRequest request, int userId);
        Task<ApiResponse<OrderResponse>> UpdateStatusAsync(int id, UpdateOrderStatusRequest updateOrderStatusRequest);
        Task<ApiResponse<bool>> CancelAsync(int id, int userId);
    }
}
