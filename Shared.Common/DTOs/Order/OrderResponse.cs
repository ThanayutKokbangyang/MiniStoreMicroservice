using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Common.DTOs.Order
{
    public record OrderResponse
    (
        int Id,
        string OrderNumber,
        decimal TotalAmount,
        string Status,
        string ShippingAddress,
        DateTime CreatedAt,
        List<OrderItemResponse> Items
    );
}
